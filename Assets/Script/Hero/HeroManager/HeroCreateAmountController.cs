using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 근접/원거리 생성 아이콘을 클릭하면 뜨는 수량 선택 모달. 슬라이더<->입력창 동기화와 수량에
// 비례한 자원 표시만 담당하고, 실제 비용 차감/영웅 생성/인구 처리는 HeroSetPanel이 넘겨준
// onBuy 콜백에서 처리한다(AddCitizen.cs의 열기/닫기/입력 파싱 컨벤션을 그대로 따른다).
public class HeroCreateAmountController : MonoBehaviour
{
    [SerializeField] private Slider amountSlider;
    [SerializeField] private TMP_InputField amountInput;
    [SerializeField] private Button buyButton;
    [SerializeField] private GameObject resourceContainer;
    [SerializeField] private HeroUpgradeResourcesUI resourceRowPrefab;
    [SerializeField] private Key closeKey = Key.Escape;

    private readonly List<HeroUpgradeResourcesUI> rows = new();
    private (ProductionType Type, int Amount)[] unitCost;
    private List<ResourceIcon> resourceIcons;
    private Action<int> onBuy;
    private int maxAmount;
    private int amount;
    private bool syncing;
    private int openedFrame;
    private RectTransform rectTransform;
    private Keyboard keyboard;
    private Mouse mouse;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
        amountSlider.wholeNumbers = true;
        amountSlider.onValueChanged.AddListener(OnSliderChanged);
        amountInput.onValueChanged.AddListener(OnInputChanged);
        buyButton.onClick.AddListener(OnBuyClicked);
    }

    public void Open(List<ResourceIcon> resourceIcons, (ProductionType Type, int Amount)[] unitCost,
        int maxAmount, bool canUseCitizen, Action<int> onBuy)
    {
        this.resourceIcons = resourceIcons;
        this.unitCost = unitCost;
        this.onBuy = onBuy;
        amount = 1;

        gameObject.SetActive(true);
        RefreshMax(maxAmount, canUseCitizen);

        keyboard = Keyboard.current;
        mouse = Mouse.current;
        openedFrame = Time.frameCount;
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    // 패널이 열려있는 동안 자원/인구가 바뀌면 HeroSetPanel이 이걸로 최대치와 구매 가능 여부를 갱신한다.
    public void RefreshMax(int maxAmount, bool canUseCitizen)
    {
        this.maxAmount = Mathf.Max(0, maxAmount);
        amountSlider.minValue = this.maxAmount > 0 ? 1 : 0;
        amountSlider.maxValue = this.maxAmount;
        buyButton.interactable = this.maxAmount > 0 && canUseCitizen;
        SetAmount(amount);
    }

    private void Update()
    {
        if (keyboard == null || mouse == null) return;
        if (keyboard[closeKey].wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)
        {
            Close();
            return;
        }

        if (Time.frameCount == openedFrame) return; // 열린 바로 그 프레임의 클릭은 무시
        if (!mouse.leftButton.wasPressedThisFrame) return;

        var point = mouse.position.ReadValue();
        if (!RectTransformUtility.RectangleContainsScreenPoint(rectTransform, point, null))
        {
            Close();
        }
    }

    private void OnSliderChanged(float value)
    {
        if (syncing) return;
        SetAmount(Mathf.RoundToInt(value));
    }

    private void OnInputChanged(string text)
    {
        if (syncing) return;
        SetAmount(int.TryParse(text, out int value) ? value : amount);
    }

    private void SetAmount(int value)
    {
        amount = Mathf.Clamp(value, (int)amountSlider.minValue, maxAmount);

        syncing = true;
        amountSlider.value = amount;
        amountInput.text = amount.ToString();
        syncing = false;

        RefreshCostDisplay();
    }

    private void RefreshCostDisplay()
    {
        if (unitCost == null) return;

        var scaled = unitCost.Multiply(amount);
        for (int i = 0; i < scaled.Length; i++)
        {
            HeroUpgradeResourcesUI row = i < rows.Count ? rows[i] : CreateRow();
            row.gameObject.SetActive(true);
            row.SetIcon(FindIcon(resourceIcons, scaled[i].Type));
            row.SetAmount(-scaled[i].Amount);
        }
        for (int i = scaled.Length; i < rows.Count; i++)
            rows[i].gameObject.SetActive(false);
    }

    private HeroUpgradeResourcesUI CreateRow()
    {
        HeroUpgradeResourcesUI row = Instantiate(resourceRowPrefab, resourceContainer.transform);
        rows.Add(row);
        return row;
    }

    private static Sprite FindIcon(List<ResourceIcon> icons, ProductionType type)
    {
        foreach (ResourceIcon i in icons)
            if (i.type == type) return i.icon;
        return null;
    }

    private void OnBuyClicked()
    {
        int bought = amount;
        onBuy?.Invoke(bought);
        Close();
    }
}
