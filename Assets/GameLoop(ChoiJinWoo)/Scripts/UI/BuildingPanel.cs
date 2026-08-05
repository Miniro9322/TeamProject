using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class BuildingPanel : MonoBehaviour, IClosablePanel
{
    [SerializeField] private TextMeshProUGUI workerText;
    [SerializeField] private Image productIcon;
    [SerializeField] private TextMeshProUGUI perProductText;
    [SerializeField] private List<CostAmountView> upgradeCostRows; // 최대 개수만큼 미리 배치, 남는 칸은 자동으로 숨김
    [SerializeField] private TextMeshProUGUI FacilityLevelText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private RegionDetailPanel parentPanel; // 이 패널을 여는 쪽 - 그 안의 슬롯 버튼 클릭은 "바깥 클릭"이 아니다
    private ProductionFacility facility;
    private RegionFacilitySlots region;
    private int slotIndex;
    private UiPanelStack panelStack;
    private BaseConstructor constructor;
    private ResourceIconSet resourceIconSet;
    private ClickOutsideCloser outsideCloser;

    [Inject]
    private void Construct(UiPanelStack panelStack, BaseConstructor constructor, ResourceIconSet resourceIconSet)
    {
        this.panelStack = panelStack;
        this.constructor = constructor;
        this.resourceIconSet = resourceIconSet;
    }

    private void Awake()
    {
        // parentPanel(RegionDetailPanel) 안의 다른 슬롯 버튼을 눌러도 "바깥 클릭"으로 안 잡히게 self로
        // 취급한다 - 안 그러면 다른 지어진 칸 클릭 -> 여기 Update()가 먼저 Close() -> 곧이어
        // RegionDetailPanel이 다시 SetActive(true)하는 순서가 되어 매번 깜빡였다.
        outsideCloser = new ClickOutsideCloser((RectTransform)transform, parentPanel != null ? parentPanel.transform : null);
    }

    private void OnEnable()
    {
        panelStack.Push(this);
        outsideCloser.MarkOpened();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (outsideCloser.ClickedOutside()) Close();
    }

    public void OnMinusButton()
    {
        if(facility != null)
        {
            facility.DecreaseWorker();
        }
    }

    public void OnPlusButton()
    {
        if (facility != null)
        {
            facility.IncreaseWorker();
        }
    }

    private void UpdatePanel()
    {
        if (workerText == null || perProductText == null || FacilityLevelText == null) return;
        workerText.text = $"{facility.WorkerAmount}/{facility.MaxWorker}";
        upgradeButton.interactable = facility.CheckCanUpgrade();

        if (productIcon != null) productIcon.sprite = resourceIconSet.GetIcon(facility.ProductionType);
        perProductText.text = $"{facility.ProductAmount * facility.WorkerAmount}/day";

        var costs = facility.UpgradeCostCopy;
        for (int i = 0; i < upgradeCostRows.Count; i++)
        {
            if (i < costs.Length)
            {
                upgradeCostRows[i].Show(resourceIconSet.GetIcon(costs[i].Type), $"{-costs[i].Amount}");
            }
            else
            {
                upgradeCostRows[i].Hide();
            }
        }

        FacilityLevelText.text = $"Lv. {facility.UpgradeCount}";
    }

    public void InitFacilityInfo(ProductionFacility facility, RegionFacilitySlots region, int slotIndex)
    {
        // 이미 열려있던 채로 다른 칸을 골랐을 수 있으니, 이전 시설 구독부터 정리한다.
        if (this.facility != null) this.facility.OnWorkerChanged -= UpdatePanel;

        this.facility = facility;
        this.region = region;
        this.slotIndex = slotIndex;
        facility.OnWorkerChanged += UpdatePanel;
        UpdatePanel();
    }

    private void OnDisable()
    {
        panelStack.Remove(this);

        if(facility != null)
        {
            facility.OnWorkerChanged -= UpdatePanel;
            facility = null;
        }
    }

    public void OnUpgrade()
    {
        facility.Upgrade();
    }

    public void OnDemolish()
    {
        if (region == null) return;

        constructor.Demolish(region, slotIndex);
        Close();
    }
}
