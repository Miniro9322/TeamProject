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
    // 상황(예: HeroMergeMention처럼 문구+버튼까지 있는데 타겟이 화면 대부분을 차지)에서는 화면 밖으로
    // 나가는 것보다 타겟 쪽으로 살짝 겹치는 걸 감수한다. 반대로 실제 target 클릭으로 완료되는
    // 단계는 겹치면 클릭을 막아버리니 화면 경계보다 타겟 회피가 항상 우선이어야 한다.
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
        // 좌우 여유가 넓어 보여도(예: 360px) 안내창 고정 폭(예: 820px)보다 작아서 못 들어가면,
        // 상/하처럼 실제로 들어가는 방향을 우선해야 화면 밖으로 밀려나거나 타겟과 겹치지 않는다.
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

    // TooltipUi.ClampToCanvas와 같은 방식으로 화면 밖으로 잘려나가지 않게 누르되, 안내창이 향하는
    // 쪽(pivot이 타겟을 바라보는 축)에서는 화면 경계보다 타겟 사각형을 우선 존중해서 절대 겹치지
    // 않게 한다 - target을 실제로 클릭해야 다음 단계로 넘어가는 경우 겹치면 클릭이 막혀버리기 때문.
    // 반대로 completesOnAcknowledge 단계("다음" 버튼으로 넘어가서 target 클릭이 필요 없는 경우, 이
    // targetClickRequired == false)는 문구+버튼까지 들어가 박스가 커지면 타겟 회피 여유 공간
    // (HeroInventory처럼 타겟이 화면 대부분을 차지하면 상하좌우 여백이 다 좁다) 안에 다 못 들어갈
    // 수 있는데, 이때는 화면 밖으로 나가 문구가 잘리는 것보다 타겟과 살짝 겹치는 쪽을 택한다 -
    // 어차피 그 자리를 클릭할 필요가 없으니 잠깐 겹쳐도 진행에 지장이 없다.
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
        if (messageBox.pivot.x == 1f) // 타겟 좌측에 배치 - 오른쪽 경계(=박스 우측)가 타겟 좌측을 넘지 못한다.
        {
            if (targetClickRequired) maxX = Mathf.Min(maxX, targetLocal.xMin);
            x = minX <= maxX ? Mathf.Clamp(desired.x, minX, maxX) : (targetClickRequired ? maxX : minX);
        }
        else if (messageBox.pivot.x == 0f) // 타겟 우측에 배치 - 왼쪽 경계(=박스 좌측)가 타겟 우측을 넘지 못한다.
        {
            if (targetClickRequired) minX = Mathf.Max(minX, targetLocal.xMax);
            x = minX <= maxX ? Mathf.Clamp(desired.x, minX, maxX) : minX;
        }
        else
        {
            x = minX <= maxX ? Mathf.Clamp(desired.x, minX, maxX) : minX;
        }

        float y;
        if (messageBox.pivot.y == 0f) // 타겟 상단에 배치 - 아래쪽 경계(=박스 하단)가 타겟 상단을 넘지 못한다.
        {
            if (targetClickRequired) minY = Mathf.Max(minY, targetLocal.yMax);
            y = minY <= maxY ? Mathf.Clamp(desired.y, minY, maxY) : minY;
        }
        else if (messageBox.pivot.y == 1f) // 타겟 하단에 배치 - 위쪽 경계(=박스 상단)가 타겟 하단을 넘지 못한다.
        {
            if (targetClickRequired) maxY = Mathf.Min(maxY, targetLocal.yMin);
            y = minY <= maxY ? Mathf.Clamp(desired.y, minY, maxY) : (targetClickRequired ? maxY : minY);
        }
        else
        {
            y = minY <= maxY ? Mathf.Clamp(desired.y, minY, maxY) : minY;
        }

        return new Vector2(x, y);
    }
}
