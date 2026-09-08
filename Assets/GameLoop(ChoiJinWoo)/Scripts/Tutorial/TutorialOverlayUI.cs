using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialOverlayUI : MonoBehaviour
{
    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private RectTransform dimmerRoot;
    [SerializeField] private RectTransform dimTop;
    [SerializeField] private RectTransform dimBottom;
    [SerializeField] private RectTransform dimLeft;
    [SerializeField] private RectTransform dimRight;
    [SerializeField] private RectTransform fullscreenBlocker;
    [SerializeField] private RectTransform messageBox;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button acknowledgeButton;
    [SerializeField] private Vector2 messageOffset = new(24f, 24f);

    [Tooltip("타겟 사각형 둘레를 두르는 강조 테두리 4장(상하좌우). dimTop/Bottom/Left/Right와 같은 " +
        "구조(anchorMin=anchorMax=(0.5,0.5))로 dimmerRoot 밑에 두되, raycastTarget은 꺼서 타겟 클릭을 " +
        "가로채지 않게 한다 - 실제 클릭 차단은 여전히 투명해진 dim 4장이 담당한다.")]
    [SerializeField] private RectTransform highlightTop;
    [SerializeField] private RectTransform highlightBottom;
    [SerializeField] private RectTransform highlightLeft;
    [SerializeField] private RectTransform highlightRight;
    [SerializeField] private float highlightBorderThickness = 4f;
    [SerializeField] private Color highlightBorderColor = new(1f, 0.82f, 0.2f, 1f);

    [Tooltip("테두리가 숨쉬듯 밝아졌다 옅어지는 펄스 속도/최저 밝기(0~1, highlightBorderColor의 알파에 곱해짐). " +
        "Time.unscaledTime 기준이라 튜토리얼이 Time.timeScale을 0으로 멈추는 구간(pauseTimeWhileActive)에도 " +
        "계속 애니메이션된다.")]
    [SerializeField] private float highlightPulseSpeed = 2.5f;
    [SerializeField, Range(0f, 1f)] private float highlightPulseMinAlpha = 0.35f;

    [Tooltip("한 줄에 허용할 최대 글자 수(공백 포함). messageBox가 ContentSizeFitter(PreferredSize)로 " +
        "텍스트 폭에 맞춰 늘어나는 구조라 TMP 자동 줄바꿈이 걸리지 않으므로, 표시 직전에 이 길이를 " +
        "넘는 줄을 직접 잘라 넣는다. StringTable에 저작해둔 \\n(문단 구분)은 그대로 존중한다.")]
    [SerializeField] private int maxLineLength = 20;

    public event Action AcknowledgeClicked;

    private bool targetClickRequired;

    private void Awake()
    {
        acknowledgeButton.onClick.AddListener(() => AcknowledgeClicked?.Invoke());

        HideDimVisually(dimTop);
        HideDimVisually(dimBottom);
        HideDimVisually(dimLeft);
        HideDimVisually(dimRight);
        HideDimVisually(fullscreenBlocker);

        SetupHighlightBorder(highlightTop);
        SetupHighlightBorder(highlightBottom);
        SetupHighlightBorder(highlightLeft);
        SetupHighlightBorder(highlightRight);

        gameObject.SetActive(false);
    }

    private static void HideDimVisually(RectTransform rt)
    {
        if (rt != null && rt.TryGetComponent(out Image image))
        {
            Color c = image.color;
            c.a = 0f;
            image.color = c;
        }
    }

    private void SetupHighlightBorder(RectTransform rt)
    {
        if (rt != null && rt.TryGetComponent(out Image image))
        {
            image.color = highlightBorderColor;
            image.raycastTarget = false;
        }
    }

    public void Show(bool showAcknowledgeButton)
    {
        gameObject.SetActive(true);
        acknowledgeButton.gameObject.SetActive(showAcknowledgeButton);
        targetClickRequired = !showAcknowledgeButton;
    }

    public void SetMessage(string messageKey)
    {
        string message = DataTableManager.StringTable.Get(messageKey);
        messageText.text = WrapLongLines(message, maxLineLength);
        LayoutRebuilder.ForceRebuildLayoutImmediate(messageBox);
    }

    private static string WrapLongLines(string text, int maxLineLength)
    {
        string[] paragraphs = text.Split('\n');
        for (int i = 0; i < paragraphs.Length; i++)
        {
            paragraphs[i] = WrapParagraph(paragraphs[i], maxLineLength);
        }
        return string.Join("\n", paragraphs);
    }

    private static string WrapParagraph(string paragraph, int maxLineLength)
    {
        if (paragraph.Length <= maxLineLength) return paragraph;

        var sb = new StringBuilder(paragraph.Length + 4);
        int lineLength = 0;
        string[] words = paragraph.Split(' ');
        foreach (string word in words)
        {
            if (lineLength > 0 && lineLength + 1 + word.Length > maxLineLength)
            {
                sb.Append('\n');
                lineLength = 0;
            }
            else if (lineLength > 0)
            {
                sb.Append(' ');
                lineLength += 1;
            }

            int index = 0;
            while (word.Length - index > maxLineLength - lineLength)
            {
                int take = Mathf.Max(1, maxLineLength - lineLength);
                sb.Append(word, index, take);
                sb.Append('\n');
                index += take;
                lineLength = 0;
            }
            sb.Append(word, index, word.Length - index);
            lineLength += word.Length - index;
        }
        return sb.ToString();
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
        SetHighlightActive(false);
    }

    public void SetSpotlight(RectTransform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            fullscreenBlocker.gameObject.SetActive(true);
            dimTop.gameObject.SetActive(false);
            dimBottom.gameObject.SetActive(false);
            dimLeft.gameObject.SetActive(false);
            dimRight.gameObject.SetActive(false);
            SetHighlightActive(false);

            PositionMessageBoxCenter();
            return;
        }

        fullscreenBlocker.gameObject.SetActive(false);
        dimTop.gameObject.SetActive(true);
        dimBottom.gameObject.SetActive(true);
        dimLeft.gameObject.SetActive(true);
        dimRight.gameObject.SetActive(true);
        SetHighlightActive(true);

        Rect targetLocal = ClampRectToBounds(ComputeLocalRect(target), dimmerRoot.rect);
        LayoutDimmers(targetLocal);
        LayoutHighlightBorder(targetLocal);
        PositionMessageBox(targetLocal);
    }

    private void SetHighlightActive(bool active)
    {
        highlightTop.gameObject.SetActive(active);
        highlightBottom.gameObject.SetActive(active);
        highlightLeft.gameObject.SetActive(active);
        highlightRight.gameObject.SetActive(active);
    }

    private void Update()
    {
        if (!highlightTop.gameObject.activeInHierarchy) return;

        float wave = (Mathf.Sin(Time.unscaledTime * highlightPulseSpeed) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(highlightPulseMinAlpha, 1f, wave) * highlightBorderColor.a;
        SetHighlightAlpha(alpha);
    }

    private void SetHighlightAlpha(float alpha)
    {
        SetImageAlpha(highlightTop, alpha);
        SetImageAlpha(highlightBottom, alpha);
        SetImageAlpha(highlightLeft, alpha);
        SetImageAlpha(highlightRight, alpha);
    }

    private void SetImageAlpha(RectTransform rt, float alpha)
    {
        if (rt != null && rt.TryGetComponent(out Image image))
        {
            Color c = highlightBorderColor;
            c.a = alpha;
            image.color = c;
        }
    }

    private Rect ComputeLocalRect(RectTransform target)
    {
        Canvas.ForceUpdateCanvases();

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

    private static Rect ClampRectToBounds(Rect r, Rect bounds)
    {
        float xMin = Mathf.Clamp(r.xMin, bounds.xMin, bounds.xMax);
        float xMax = Mathf.Clamp(r.xMax, bounds.xMin, bounds.xMax);
        float yMin = Mathf.Clamp(r.yMin, bounds.yMin, bounds.yMax);
        float yMax = Mathf.Clamp(r.yMax, bounds.yMin, bounds.yMax);
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private void LayoutDimmers(Rect t)
    {
        Rect full = dimmerRoot.rect;

        SetRect(dimTop, full.xMin, t.yMax, full.xMax, full.yMax);
        SetRect(dimBottom, full.xMin, full.yMin, full.xMax, t.yMin);
        SetRect(dimLeft, full.xMin, t.yMin, t.xMin, t.yMax);
        SetRect(dimRight, t.xMax, t.yMin, full.xMax, t.yMax);
    }

    private void LayoutHighlightBorder(Rect t)
    {
        float half = highlightBorderThickness * 0.5f;
        SetRect(highlightTop, t.xMin - half, t.yMax - half, t.xMax + half, t.yMax + half);
        SetRect(highlightBottom, t.xMin - half, t.yMin - half, t.xMax + half, t.yMin + half);
        SetRect(highlightLeft, t.xMin - half, t.yMin - half, t.xMin + half, t.yMax + half);
        SetRect(highlightRight, t.xMax - half, t.yMin - half, t.xMax + half, t.yMax + half);
    }

    private static void SetRect(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
    {
        float width = Mathf.Max(0f, xMax - xMin);
        float height = Mathf.Max(0f, yMax - yMin);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = new Vector2((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f);
    }

    private void PositionMessageBoxCenter()
    {
        messageBox.pivot = new Vector2(0.5f, 0.5f);
        messageBox.anchoredPosition = dimmerRoot.rect.center;
    }

    public void PositionMessageBoxBottomCenter()
    {
        Rect bounds = dimmerRoot.rect;
        messageBox.pivot = new Vector2(0.5f, 0f);
        messageBox.anchoredPosition = new Vector2(bounds.center.x, bounds.yMin + messageOffset.y);
    }

    private void PositionMessageBox(Rect targetLocal)
    {
        Rect bounds = dimmerRoot.rect;
        Vector2 size = messageBox.rect.size;

        float spaceTop = bounds.yMax - targetLocal.yMax;
        float spaceBottom = targetLocal.yMin - bounds.yMin;
        float spaceLeft = targetLocal.xMin - bounds.xMin;
        float spaceRight = bounds.xMax - targetLocal.xMax;

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
        if (messageBox.pivot.x == 1f)
        {
            float? preferredMax = targetClickRequired ? Mathf.Min(maxX, targetLocal.xMin) : (float?)null;
            x = ClampPreferring(desired.x, minX, maxX, null, preferredMax);
        }
        else if (messageBox.pivot.x == 0f)
        {
            float? preferredMin = targetClickRequired ? Mathf.Max(minX, targetLocal.xMax) : (float?)null;
            x = ClampPreferring(desired.x, minX, maxX, preferredMin, null);
        }
        else
        {
            x = ClampPreferring(desired.x, minX, maxX, null, null);
        }

        float y;
        if (messageBox.pivot.y == 0f)
        {
            float? preferredMin = targetClickRequired ? Mathf.Max(minY, targetLocal.yMax) : (float?)null;
            y = ClampPreferring(desired.y, minY, maxY, preferredMin, null);
        }
        else if (messageBox.pivot.y == 1f)
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

    private static float ClampPreferring(float desired, float screenMin, float screenMax, float? preferredMin, float? preferredMax)
    {
        float rMin = preferredMin ?? screenMin;
        float rMax = preferredMax ?? screenMax;
        if (rMin <= rMax) return Mathf.Clamp(desired, rMin, rMax);
        if (screenMin <= screenMax) return Mathf.Clamp(desired, screenMin, screenMax);
        return screenMin;
    }
}
