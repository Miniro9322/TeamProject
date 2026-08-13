using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 씬에 하나만 두는 툴팁 창. TooltipTrigger들이 Instance로 직접 호출해서 띄우고 끈다.
public class TooltipUi : MonoBehaviour
{
    public static TooltipUi Instance { get; private set; }

    [SerializeField] private RectTransform panel;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private Canvas canvas; // 화면 좌표 -> 캔버스 로컬 좌표 변환에 필요
    [SerializeField] private Vector2 offset = new(16f, 16f);
    [SerializeField] private float fadeDuration = 0.12f;

    private RectTransform canvasRect;
    private CanvasGroup canvasGroup;
    private CancellationTokenSource fadeCts;

    private void Awake()
    {
        Instance = this;
        canvasRect = canvas.transform as RectTransform;

        // panel이 마우스 밑에 뜨면서 자기가 레이캐스트를 가로채면, 밑에 있던 버튼이 PointerExit ->
        // 패널 사라짐 -> 버튼 PointerEnter -> 패널 다시 뜸 순으로 매 프레임 깜빡인다.
        // CanvasGroup으로 패널이 절대 레이캐스트를 막지 않게 고정해서 이 루프 자체를 없앤다.
        canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = panel.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        canvasGroup.alpha = 0f;

        // pivot이 인스펙터 설정값(예: 센터)에 따라 달라지면 offset을 줘도 커서가 패널 안쪽에 걸릴 수 있다.
        // 좌하단(0,0)으로 고정해서 "커서 지점에서 오른쪽 위로 offset만큼 벌어진 자리"가 항상
        // 패널의 시작 모서리가 되게 하고, 패널 전체가 커서 위쪽으로만 펼쳐지게 한다(화살표 커서는
        // 보통 tip에서 오른쪽 아래로 향하므로, 위로 띄우면 커서 모양과 안 겹친다).
        panel.pivot = new Vector2(0f, 0f);

        panel.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Show(string message, Vector2 screenPosition)
    {
        if (string.IsNullOrEmpty(message)) return;

        text.text = DataTableManager.StringTable.Get(message);
        panel.gameObject.SetActive(true);

        // text.text를 바꿔도 ContentSizeFitter/레이아웃은 다음 갱신 때야 반영되므로, 그 상태로 바로
        // 클램핑하면 panel.rect.size가 이전 텍스트 기준 크기라 화면 밖으로 새는 계산이 나온다.
        // 여기서 강제로 즉시 재계산시켜서 SetPosition이 최신 크기를 보게 한다.
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        SetPosition(screenPosition);

        FadeTo(1f).Forget();
    }

    public void Hide()
    {
        if (!panel.gameObject.activeSelf) return;
        FadeTo(0f).Forget();
    }

    // 페이드 도중 다시 Show/Hide가 불리면 진행 중이던 페이드를 취소하고 새 목표값으로 다시 시작한다.
    private async UniTaskVoid FadeTo(float target)
    {
        fadeCts?.Cancel();
        fadeCts = new CancellationTokenSource();
        var token = fadeCts.Token;

        float start = canvasGroup.alpha;
        float elapsed = 0f;

        try
        {
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }

        canvasGroup.alpha = target;
        if (target <= 0f) panel.gameObject.SetActive(false);
    }

    private void SetPosition(Vector2 screenPosition)
    {
        var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, cam, out var localPoint);
        panel.anchoredPosition = ClampToCanvas(localPoint + offset);
    }

    // 패널이 화면(캔버스) 밖으로 잘려나가지 않도록 anchoredPosition을 캔버스 경계 안으로 눌러 담는다.
    // panel의 anchorMin == anchorMax(늘어나지 않는 고정 앵커)인 일반적인 툴팁 설정을 가정한다.
    private Vector2 ClampToCanvas(Vector2 desired)
    {
        var bounds = canvasRect.rect;
        var anchorPoint = bounds.min + Vector2.Scale(bounds.size, panel.anchorMin);
        var pivotOffset = Vector2.Scale(panel.rect.size, panel.pivot);

        var minX = bounds.xMin - anchorPoint.x + pivotOffset.x;
        var maxX = bounds.xMax - anchorPoint.x - (panel.rect.width - pivotOffset.x);
        var minY = bounds.yMin - anchorPoint.y + pivotOffset.y;
        var maxY = bounds.yMax - anchorPoint.y - (panel.rect.height - pivotOffset.y);

        // 패널이 캔버스보다 크면 min > max가 되어버리니, 그런 경우엔 최소값에 고정한다.
        float x = minX <= maxX ? Mathf.Clamp(desired.x, minX, maxX) : minX;
        float y = minY <= maxY ? Mathf.Clamp(desired.y, minY, maxY) : minY;
        return new Vector2(x, y);
    }
}
