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
    [SerializeField] private TextMeshProUGUI NextLevelInfoText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private GameObject workerButtonsContainer; // +/- 인력 버튼을 감싸는 오브젝트 - House에는 없는 개념이라 통째로 숨김
    [SerializeField] private RegionDetailPanel parentPanel; // 이 패널을 여는 쪽 - 그 안의 슬롯 버튼 클릭은 "바깥 클릭"이 아니다
    private ProductionFacility facility;
    private House house;
    private IUpgradableOccupant Occupant => facility != null ? (IUpgradableOccupant)facility : house;
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
        var occupant = Occupant;
        if (occupant == null || FacilityLevelText == null) return;

        // 인력/생산량은 ProductionFacility에만 있는 개념이라, House를 보는 중이면 숨긴다.
        bool isFacility = facility != null;
        if (workerText != null) workerText.gameObject.SetActive(isFacility);
        if (productIcon != null) productIcon.gameObject.SetActive(isFacility);
        if (perProductText != null) perProductText.gameObject.SetActive(isFacility);
        if (workerButtonsContainer != null) workerButtonsContainer.SetActive(isFacility);

        if (isFacility)
        {
            if (workerText != null) workerText.text = $"{facility.WorkerAmount}/{facility.MaxWorker}";
            if (productIcon != null) productIcon.sprite = resourceIconSet.GetIcon(facility.ProductionType);
            if (perProductText != null) perProductText.text = $"{facility.ProductAmount * facility.WorkerAmount}/day";
        }

        upgradeButton.interactable = occupant.CheckCanUpgrade();

        var costs = occupant.UpgradeCostCopy;
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

        FacilityLevelText.text = $"Lv. {occupant.UpgradeCount}";
        if (NextLevelInfoText != null) NextLevelInfoText.text = $"Lv. {(occupant.UpgradeCount != occupant.MaxUpgrade ? occupant.UpgradeCount : "Max")}" +
                $"{(occupant.UpgradeCount != occupant.MaxUpgrade ? $"→ Lv. {occupant.UpgradeCount + 1}" : string.Empty)}\n{occupant.NextUpgradeInfo}";
    }

    // ProductionFacility/House 어느 쪽이든 이 하나로 받는다 - 둘 다 IUpgradableOccupant라
    // 레벨/강화 UI 쪼는 공통으로 그리고, 인력 UI 쪼만 facility일 때 추가로 채운다.
    public void InitOccupant(object occupant, RegionFacilitySlots region, int slotIndex)
    {
        // 이미 열려있던 채로 다른 칸을 골랐을 수 있으니, 이전 점유물 구독부터 정리한다.
        UnsubscribeOccupant();

        facility = occupant as ProductionFacility;
        house = occupant as House;
        this.region = region;
        this.slotIndex = slotIndex;

        var current = Occupant;
        if (current != null) current.Changed += UpdatePanel;
        UpdatePanel();
    }

    private void UnsubscribeOccupant()
    {
        var current = Occupant;
        if (current != null) current.Changed -= UpdatePanel;
    }

    private void OnDisable()
    {
        panelStack.Remove(this);

        UnsubscribeOccupant();
        facility = null;
        house = null;
    }

    public void OnUpgrade()
    {
        Occupant?.Upgrade();
    }

    public void OnDemolish()
    {
        if (region == null) return;

        constructor.Demolish(region, slotIndex);
        Close();
    }
}
