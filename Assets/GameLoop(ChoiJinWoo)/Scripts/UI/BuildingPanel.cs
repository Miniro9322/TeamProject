using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class BuildingPanel : MonoBehaviour, IClosablePanel
{
    [SerializeField] private TextMeshProUGUI workerText;
    [SerializeField] private TextMeshProUGUI perProductText;
    [SerializeField] private TextMeshProUGUI upgradeCostText;
    [SerializeField] private TextMeshProUGUI FacilityLevelText;
    [SerializeField] private Button upgradeButton;
    private ProductionFacility facility;
    private RegionFacilitySlots region;
    private int slotIndex;
    private UiPanelStack panelStack;
    private BaseConstructor constructor;
    private ClickOutsideCloser outsideCloser;

    [Inject]
    private void Construct(UiPanelStack panelStack, BaseConstructor constructor)
    {
        this.panelStack = panelStack;
        this.constructor = constructor;
    }

    private void Awake()
    {
        outsideCloser = new ClickOutsideCloser((RectTransform)transform);
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
        if (workerText == null || perProductText == null || upgradeCostText == null || FacilityLevelText == null) return;
        workerText.text = $"{facility.WorkerAmount}/{facility.MaxWorker}";
        upgradeButton.interactable = facility.CheckCanUpgrade();
        perProductText.text = $"{facility.ProductionType} {facility.ProductAmount * facility.WorkerAmount}/day";
        var sb = new StringBuilder();
        foreach(var cost in facility.UpgradeCostCopy)
        {
            sb.Append($"{cost.Type} : {-cost.Amount}\n");
        }
        sb.Length--;
        upgradeCostText.text = sb.ToString();
        FacilityLevelText.text = $"Lv. {facility.UpgradeCount}";
    }

    public void InitFacilityInfo(ProductionFacility facility, RegionFacilitySlots region, int slotIndex)
    {
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
