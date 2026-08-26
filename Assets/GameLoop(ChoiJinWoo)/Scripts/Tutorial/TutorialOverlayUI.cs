using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 화면을 딤 처리하되 스포트라이트 대상(target)만 뚫어두는 튜토리얼 오버레이.
// 딤 이미지 4장(상하좌우)으로 타겟 사각형 둘레만 감싸는 "액자" 형태라 타겟 위에는 아무것도
// 그려지지 않고, 그래서 GraphicRaycaster가 자연스럽게 실제 타겟 버튼으로 클릭을 통과시킨다.
//
// 인스펙터 요구사항: dimmerRoot는 부모(overlayCanvas)에 풀스트레치 + pivot(0.5,0.5)로 두고,
// dimTop/Bottom/Left/Right/fullscreenBlocker/messageBox는 전부 dimmerRoot의 자식으로
// anchorMin=anchorMax=(0.5,0.5)로 둔다 - 그래야 dimmerRoot.rect의 로컬 좌표가 곧 anchoredPosition이 된다.
public class TutorialOverlayUI : MonoBehaviour
{
    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private RectTransform dimmerRoot;
    [SerializeField] private RectTransform dimTop;
    [SerializeField] private RectTransform dimBottom;
    [SerializeField] private RectTransform dimLeft;
    [SerializeField] private RectTransform dimRight;
    [SerializeField] private RectTransform fullscreenBlocker; // target이 하나도 안 잡힐 때(패널 전환 도중 등) 전체 차단 폴백
    [SerializeField] private RectTransform messageBox;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button acknowledgeButton; // "다음" - completesOnAcknowledge 단계에서만 보인다
    [SerializeField] private Vector2 messageOffset = new(24f, 24f);

    public event Action AcknowledgeClicked;

    // "다음" 버튼으로 넘어가는 단계는 진행에 타겟 클릭이 필요 없으니, 안내창이 도저히 안 들어가는
    // 상황에서는 화면 밖으로 나가는 것보다 타겟 쪽으로 살짝 겹치는 걸 감수한다(ClampPreferring 참고).
    // 반대로 실제 target 클릭으로 완료되는 단계는 겹치면 클릭이 막히니 되도록 겹치지 않으려 하되,
    // 어느 쪽이든 화면 경계를 벗어나는 것보다는 우선순위가 낮다 - 안내창이 아예 안 보이는 것보다
    // 타겟과 살짝 겹치는 채로라도 화면 안에 보이는 쪽이 낫다.
    private bool targetClickRequired;

    private void Awake()
    {
        acknowledgeButton.onClick.AddListener(() => AcknowledgeClicked?.Invoke());
        gameObject.SetActive(false);
    }

    public void Show(bool showAcknowledgeButton)
    {
        gameObject.SetActive(true);
        acknowledgeButton.gameObject.SetActive(showAcknowledgeButton);
        targetClickRequired = !showAcknowledgeButton;
    }

    public void SetMessage(string messageKey)
    {
        messageText.text = DataTableManager.StringTable.Get(messageKey);
        LayoutRebuilder.ForceRebuildLayoutImmediate(messageBox);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void ShowUnblocked()
    {
        dimTop.gameObject.SetActive(false);
        dimBottom.gameObject.SetActive(false);
        dimLeft.gameObject.SetActive(false);
        dimRight.gameObject.SetActive(false);
        fullscreenBlocker.gameObject.SetActive(false);
    }

    // 매 프레임 TutorialManager가 호출한다 - target이 패널 토글로 나타났다 사라졌다 하므로 매번 다시 계산한다.
    public void SetSpotlight(RectTransform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            fullscreenBlocker.gameObject.SetActive(true);
            dimTop.gameObject.SetActive(false);
            dimBottom.gameObject.SetActive(false);
            dimLeft.gameObject.SetActive(false);
            dimRight.gameObject.SetActive(false);

            PositionMessageBoxCenter();
            return;
        }

        fullscreenBlocker.gameObject.SetActive(false);
        dimTop.gameObject.SetActive(true);
        dimBottom.gameObject.SetActive(true);
        dimLeft.gameObject.SetActive(true);
        dimRight.gameObject.SetActive(true);

        Rect targetLocal = ComputeLocalRect(target);
        LayoutDimmers(targetLocal);
        PositionMessageBox(targetLocal);
    }

    // target의 월드 코너 -> 스크린 좌표 -> dimmerRoot 로컬 좌표. 두 캔버스의 렌더 모드가 달라도 안전하다.
    private Rect ComputeLocalRect(RectTransform target)
    {
        var corners = new Vector3[4];
        target.GetWorldCorners(corners); // [0] bottom-left, [2] top-right

        Canvas targetCanvas = target.GetComponentInParent<Canvas>();
        Camera targetCam = targetCanvas == null || targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : targetCanvas.worldCamera;
        Vector2 screenMin = RectTransformUtility.WorldToScreenPoint(targetCam, corners[0]);
        Vector2 screenMax = RectTransformUtility.WorldToScreenPoint(targetCam, corners[2]);

        Camera overlayCam = overlayCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : overlayCanvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(dimmerRoot, screenMin, overlayCam, out var localMin);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(dimmerRoot, screenMax, overlayCam, out var localMax);

        return Rect.MinMaxRect(localMin.x, localMin.y, localMax.x, localMax.y);
    }

    // 타겟 사각형 둘레를 4장으로 감싼다 - 겹침도 빈틈도 없고, 타겟 자리에는 아무것도 그리지 않는다.
    private void LayoutDimmers(Rect t)
    {
        Rect full = dimmerRoot.rect;

        SetRect(dimTop, full.xMin, t.yMax, full.xMax, full.yMax);
        SetRect(dimBottom, full.xMin, full.yMin, full.xMax, t.yMin);
        SetRect(dimLeft, full.xMin, t.yMin, t.xMin, t.yMax);
        SetRect(dimRight, t.xMax, t.yMin, full.xMax, t.yMax);
    }

    private static void SetRect(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
    {
        float width = Mathf.Max(0f, xMax - xMin);
        float height = Mathf.Max(0f, yMax - yMin);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = new Vector2((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f);
    }

    // 스포트라이트 대상이 없을 때(완료 메시지 등) 화면 정중앙에 고정한다.
    private void PositionMessageBoxCenter()
    {
        messageBox.pivot = new Vector2(0.5f, 0.5f);
        messageBox.anchoredPosition = dimmerRoot.rect.center;
    }

    private void PositionMessageBox(Rect targetLocal)
    {
        Rect bounds = dimmerRoot.rect;
        Vector2 size = messageBox.rect.size;

        float spaceTop = bounds.yMax - targetLocal.yMax;
        float spaceBottom = targetLocal.yMin - bounds.yMin;
        float spaceLeft = targetLocal.xMin - bounds.xMin;
        float spaceRight = bounds.xMax - targetLocal.xMax;

        // 남는 공간이 가장 넓은 방향이 아니라, 안내창이 실제로 그 공간에 다 들어가고도 얼마나
        // 남는지(공간 - 박스 크기 - 여백)로 방향을 고른다. HeroInventory처럼 폭이 넓은 타겟은
        // 좌우 여유가 넓어 보여도 안내창 폭보다 작아서 못 들어가면, 상/하처럼 실제로 들어가는
        // 방향을 우선해야 화면 밖으로 밀려나거나 타겟과 겹치지 않는다.
        float fitTop = spaceTop - size.y - messageOffset.y;
        float fitBottom = spaceBottom - size.y - messageOffset.y;
        float fitLeft = spaceLeft - size.x - messageOffset.x;
        float fitRight = spaceRight - size.x - messageOffset.x;
        float best = Mathf.Max(Mathf.Max(fitTop, fitBottom), Mathf.Max(fitLeft, fitRight));

        Vector2 anchor;
        Vector2 offset;
        if (best == fitTop)
        {
            anchor = new Vector2(targetLocal.center.x, targetLocal.yMax);
            messageBox.pivot = new Vector2(0.5f, 0f);
            offset = new Vector2(0f, messageOffset.y);
        }
        else if (best == fitBottom)
        {
            anchor = new Vector2(targetLocal.center.x, targetLocal.yMin);
            messageBox.pivot = new Vector2(0.5f, 1f);
            offset = new Vector2(0f, -messageOffset.y);
        }
        else if (best == fitLeft)
        {
            anchor = new Vector2(targetLocal.xMin, targetLocal.center.y);
            messageBox.pivot = new Vector2(1f, 0.5f);
            offset = new Vector2(-messageOffset.x, 0f);
        }
        else
        {
            anchor = new Vector2(targetLocal.xMax, targetLocal.center.y);
            messageBox.pivot = new Vector2(0f, 0.5f);
            offset = new Vector2(messageOffset.x, 0f);
        }

        messageBox.anchoredPosition = ClampToBounds(anchor + offset, targetLocal);
    }

    // 화면 경계를 항상 최우선으로 지킨다 - 안내창이 화면 밖으로 나가는 일은 없어야 한다. 타겟과
    // 안 겹치는 것(target을 클릭해야 진행되는 단계에서 클릭을 막지 않기 위함)은 "가능하면" 지키는
    // 2순위 선호일 뿐이라, 화면 안에 두면서 동시에 타겟도 피할 공간이 없을 때는 화면 안에 두는
    // 쪽을 선택하고 타겟과의 겹침은 감수한다 - 완전히 화면 밖으로 사라지는 것보다 타겟과 살짝
    // 겹친 채로라도 보이는 편이 사용자가 진행 상황을 파악하기에 낫다.
    private Vector2 ClampToBounds(Vector2 desired, Rect targetLocal)
    {
        Rect bounds = dimmerRoot.rect;
        Vector2 anchorPoint = bounds.min + Vector2.Scale(bounds.size, messageBox.anchorMin);
        Vector2 pivotOffset = Vector2.Scale(messageBox.rect.size, messageBox.pivot);

        float minX = bounds.xMin - anchorPoint.x + pivotOffset.x;
        float maxX = bounds.xMax - anchorPoint.x - (messageBox.rect.width - pivotOffset.x);
        float minY = bounds.yMin - anchorPoint.y + pivotOffset.y;
        float maxY = bounds.yMax - anchorPoint.y - (messageBox.rect.height - pivotOffset.y);

        float x;
        if (messageBox.pivot.x == 1f) // 타겟 좌측에 배치 - 가능하면 오른쪽 경계(=박스 우측)가 타겟 좌측을 안 넘게.
        {
            float? preferredMax = targetClickRequired ? Mathf.Min(maxX, targetLocal.xMin) : (float?)null;
            x = ClampPreferring(desired.x, minX, maxX, null, preferredMax);
        }
        else if (messageBox.pivot.x == 0f) // 타겟 우측에 배치 - 가능하면 왼쪽 경계(=박스 좌측)가 타겟 우측을 안 넘게.
        {
            float? preferredMin = targetClickRequired ? Mathf.Max(minX, targetLocal.xMax) : (float?)null;
            x = ClampPreferring(desired.x, minX, maxX, preferredMin, null);
        }
        else
        {
            x = ClampPreferring(desired.x, minX, maxX, null, null);
        }

        float y;
        if (messageBox.pivot.y == 0f) // 타겟 상단에 배치 - 가능하면 아래쪽 경계(=박스 하단)가 타겟 상단을 안 넘게.
        {
            float? preferredMin = targetClickRequired ? Mathf.Max(minY, targetLocal.yMax) : (float?)null;
            y = ClampPreferring(desired.y, minY, maxY, preferredMin, null);
        }
        else if (messageBox.pivot.y == 1f) // 타겟 하단에 배치 - 가능하면 위쪽 경계(=박스 상단)가 타겟 하단을 안 넘게.
        {
            float? preferredMax = targetClickRequired ? Mathf.Min(maxY, targetLocal.yMin) : (float?)null;
            y = ClampPreferring(desired.y, minY, maxY, null, preferredMax);
        }
        else
        {
            y = ClampPreferring(desired.y, minY, maxY, null, null);
        }

        return new Vector2(x, y);
    }

    // screenMin/screenMax(화면 경계, 항상 지켜야 함) 안에서, preferredMin/preferredMax(타겟 회피,
    // 되면 좋은 것)까지 같이 만족하는 값이 있으면 그걸 쓰고, 없으면 화면 경계만 지키는 값으로
    // 물러난다. screenMin > screenMax(안내창 자체가 화면보다 큼)인 극단적인 경우에만 최소한으로 넘친다.
    private static float ClampPreferring(float desired, float screenMin, float screenMax, float? preferredMin, float? preferredMax)
    {
        float rMin = preferredMin ?? screenMin;
        float rMax = preferredMax ?? screenMax;
        if (rMin <= rMax) return Mathf.Clamp(desired, rMin, rMax);
        if (screenMin <= screenMax) return Mathf.Clamp(desired, screenMin, screenMax);
        return screenMin;
    }
}
