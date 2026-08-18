using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 씬에 하나만 두는 툴팁 창. TooltipTrigger들이 Instance로 직접 호출해서 띄우고 끈다.
public class HeroCreateToolTipUI : MonoBehaviour
{
    public static HeroCreateToolTipUI Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private RectTransform panel;
    [SerializeField] private GameObject content;
    [SerializeField] private HeroUpgradeResourcesUI resourceInfoPrefab;
    [SerializeField] private float fadeDuration = 0.12f;

    private CanvasGroup canvasGroup;
    private CancellationTokenSource fadeCts;
    private readonly List<HeroUpgradeResourcesUI> rows = new();

    private void Awake()
    {
        Instance = this;

        // panel이 마우스 밑에 뜨면서 자기가 레이캐스트를 가로채면, 밑에 있던 버튼이 PointerExit ->
        // 패널 사라짐 -> 버튼 PointerEnter -> 패널 다시 뜸 순으로 매 프레임 깜빡인다.
        // CanvasGroup으로 패널이 절대 레이캐스트를 막지 않게 고정해서 이 루프 자체를 없앤다.
        canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = panel.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        canvasGroup.alpha = 0f;

        panel.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Show(string createName, List<ResourceCost> resourceCost, List<ResourceIcon> resourceIcons)
    {
        text.text = DataTableManager.StringTable.Get(createName);

        for (int i = 0; i < resourceCost.Count; i++)
        {
            HeroUpgradeResourcesUI row = i < rows.Count ? rows[i] : CreateRow();
            row.gameObject.SetActive(true);
            row.SetIcon(FindIcon(resourceIcons, resourceCost[i].Type));
            row.SetAmount(resourceCost[i].Amount);
        }
        // 자원 개수가 이전보다 줄었으면 남는 줄은 숨긴다(재사용을 위해 파괴하지 않음).
        for (int i = resourceCost.Count; i < rows.Count; i++)
            rows[i].gameObject.SetActive(false);

        panel.gameObject.SetActive(true);

        // text.text/자원 줄이 바뀌어도 ContentSizeFitter 등 레이아웃은 다음 갱신 때야 반영되므로,
        // 여기서 강제로 즉시 재계산시켜서 크기가 바로 반영되게 한다. 위치는 씬에 배치된 그대로 둔다.
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel);

        FadeTo(1f).Forget();
    }

    public void Hide()
    {
        if (!panel.gameObject.activeSelf) return;
        FadeTo(0f).Forget();
    }

    private HeroUpgradeResourcesUI CreateRow()
    {
        HeroUpgradeResourcesUI row = Instantiate(resourceInfoPrefab, content.transform);
        rows.Add(row);
        return row;
    }

    private static Sprite FindIcon(List<ResourceIcon> icons, ProductionType type)
    {
        foreach (ResourceIcon i in icons)
            if (i.type == type) return i.icon;
        return null;
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
}
