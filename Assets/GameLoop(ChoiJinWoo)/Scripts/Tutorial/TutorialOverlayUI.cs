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

    private void Awake()
    {
        acknowledgeButton.onClick.AddListener(() => AcknowledgeClicked?.Invoke());
        gameObject.SetActive(false);
    }

    public void Show(bool showAcknowledgeButton)
    {
        gameObject.SetActive(true);
        acknowledgeButton.gameObject.SetActive(showAcknowledgeButton);
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

        float spaceTop = bounds.yMax - targetLocal.yMax;
        float spaceBottom = targetLocal.yMin - bounds.yMin;
        float spaceLeft = targetLocal.xMin - bounds.xMin;
        float spaceRight = bounds.xMax - targetLocal.xMax;
        float best = Mathf.Max(Mathf.Max(spaceTop, spaceBottom), Mathf.Max(spaceLeft, spaceRight));

        Vector2 anchor;
        Vector2 offset;
        if (best == spaceTop)
        {
            anchor = new Vector2(targetLocal.center.x, targetLocal.yMax);
            messageBox.pivot = new Vector2(0.5f, 0f);
            offset = new Vector2(0f, messageOffset.y);
        }
        else if (best == spaceBottom)
        {
            anchor = new Vector2(targetLocal.center.x, targetLocal.yMin);
            messageBox.pivot = new Vector2(0.5f, 1f);
            offset = new Vector2(0f, -messageOffset.y);
        }
        else if (best == spaceLeft)
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

        messageBox.anchoredPosition = ClampToBounds(anchor + offset);
    }

    // TooltipUi.ClampToCanvas와 같은 방식 - 메시지 박스가 화면 밖으로 잘려나가지 않게 anchoredPosition을 눌러 담는다.
    private Vector2 ClampToBounds(Vector2 desired)
    {
        Rect bounds = dimmerRoot.rect;
        Vector2 anchorPoint = bounds.min + Vector2.Scale(bounds.size, messageBox.anchorMin);
        Vector2 pivotOffset = Vector2.Scale(messageBox.rect.size, messageBox.pivot);

        float minX = bounds.xMin - anchorPoint.x + pivotOffset.x;
        float maxX = bounds.xMax - anchorPoint.x - (messageBox.rect.width - pivotOffset.x);
        float minY = bounds.yMin - anchorPoint.y + pivotOffset.y;
        float maxY = bounds.yMax - anchorPoint.y - (messageBox.rect.height - pivotOffset.y);

        float x = minX <= maxX ? Mathf.Clamp(desired.x, minX, maxX) : minX;
        float y = minY <= maxY ? Mathf.Clamp(desired.y, minY, maxY) : minY;
        return new Vector2(x, y);
    }
}
