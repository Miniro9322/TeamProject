using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
    [SerializeField] private Button demolishButton; // House는 철거를 지원하지 않아 비활성화한다
    [SerializeField] private GameObject workerButtonsContainer; // +/- 인력 버튼을 감싸는 오브젝트 - House에는 없는 개념이라 통째로 숨김
    [SerializeField] private RegionDetailPanel parentPanel; // 이 패널을 여는 쪽 - 그 안의 슬롯 버튼 클릭은 "바깥 클릭"이 아니다
    private ProductionFacility facility;
    private House house;
    private IUpgradableOccupant Occupant => facility != null ? (IUpgradableOccupant)facility : house;
    private RegionFacilitySlots region;
    private int slotIndex;
    public int SlotIndex => slotIndex;
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
        LocalizeTextManager.OnLanguageChanged += UpdatePanel;
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (panelStack.IsTop(this) && outsideCloser.ShouldClose()) Close();
    }

    public void OnMinusButton()
    {
        if(facility != null)
        {
            facility.DecreaseWorker();
        }

        // Selected 상태가 Pressed와 같은 클립이라, 포커스를 안 풀면 눌린 모양이 계속 남는다
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void OnPlusButton()
    {
        if (facility != null)
        {
            facility.IncreaseWorker();
        }

        // Selected 상태가 Pressed와 같은 클립이라, 포커스를 안 풀면 눌린 모양이 계속 남는다
        EventSystem.current.SetSelectedGameObject(null);
    }

    private void UpdatePanel()
    {
        var occupant = Occupant;
        if (occupant == null || FacilityLevelText == null) return;

        var table = DataTableManager.StringTable;

        // 인력/생산량은 ProductionFacility에만 있는 개념이라, House를 보는 중이면 숨긴다.
        bool isFacility = facility != null;
        if (workerText != null) workerText.gameObject.SetActive(isFacility);
        if (productIcon != null) productIcon.gameObject.SetActive(isFacility);
        if (perProductText != null) perProductText.gameObject.SetActive(isFacility);
        if (workerButtonsContainer != null) workerButtonsContainer.SetActive(isFacility);
        if (demolishButton != null) demolishButton.gameObject.SetActive(isFacility);

        if (isFacility)
        {
            if (workerText != null) workerText.text = $"{facility.WorkerAmount}/{facility.MaxWorker}";
            if (productIcon != null) productIcon.sprite = resourceIconSet.GetIcon(facility.ProductionType);
            if (perProductText != null) perProductText.text = $"{facility.ProductAmount * facility.WorkerAmount}{table.Get("Ui_PerDay")}";
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

        FacilityLevelText.text = string.Format(table.Get("Ui_LevelFormat"), occupant.UpgradeCount);
        if (NextLevelInfoText != null)
        {
            bool isMaxLevel = occupant.UpgradeCount == occupant.MaxUpgrade;
            string currentLevel = string.Format(table.Get("Ui_LevelFormat"), isMaxLevel ? table.Get("Ui_MaxLevel") : occupant.UpgradeCount.ToString());
            string nextLevel = isMaxLevel ? string.Empty : $"→ {string.Format(table.Get("Ui_LevelFormat"), occupant.UpgradeCount + 1)}";
            NextLevelInfoText.text = $"{currentLevel}{nextLevel}\n{table.Get(occupant.NextUpgradeInfo)}";
        }
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
        LocalizeTextManager.OnLanguageChanged -= UpdatePanel;

        UnsubscribeOccupant();
        facility = null;
        house = null;
    }

    // 튜토리얼이 "업그레이드 버튼을 실제로 눌렀는지"만 골라 판정할 수 있도록 알려준다.
    public event System.Action Upgraded;

    public void OnUpgrade()
    {
        if (Occupant == null) return;

        Occupant.Upgrade();
        Upgraded?.Invoke();

        // Selected 상태가 Pressed와 같은 클립이라, 포커스를 안 풀면 눌린 모양이 계속 남는다
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void OnDemolish()
    {
        if (region == null || facility == null) return; // House는 철거 버튼 자체를 비활성화해뒀지만, 방어적으로 한 번 더 막는다

        constructor.Demolish(region, slotIndex);
        Close();
    }
}
