using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class RegionDetailPanel : MonoBehaviour, IClosablePanel
{
    [SerializeField] private TextMeshProUGUI regionNameText;
    [SerializeField] private List<FacilitySlotView> slotViews;
    [SerializeField] private FacilityBuildChoicePanel buildChoicePanel;
    [SerializeField] private BuildingPanel buildingPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private RegionOverviewPanel overviewPanel;

    private RegionFacilitySlots region;
    private IUpgradableOccupant openOccupant;
    private UiPanelStack panelStack;
    private ClickOutsideCloser outsideCloser;
    private PanelReveal panelReveal;

    public RegionFacilitySlots CurrentRegion => gameObject.activeSelf ? region : null;

    public event System.Action RegionChanged;

    [Inject]
    private void Construct(UiPanelStack panelStack)
    {
        this.panelStack = panelStack;
        
        Transform[] nodeTransforms = null;
        if (overviewPanel != null)
        {
            var nodes = overviewPanel.Nodes;
            nodeTransforms = new Transform[nodes.Count];
            for (int i = 0; i < nodes.Count; i++) nodeTransforms[i] = nodes[i].transform;
        }
        outsideCloser = new ClickOutsideCloser((RectTransform)transform, nodeTransforms);
    }

    private void Awake()
    {
        panelReveal = GetComponent<PanelReveal>();

        for (int i = 0; i < slotViews.Count; i++)
        {
            slotViews[i].SetIndex(i);
            slotViews[i].BindClick(OnSlotClicked);
        }

        if (closeButton != null) closeButton.onClick.AddListener(Close);
        buildChoicePanel.gameObject.SetActive(false);
        buildingPanel.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        panelStack.Push(this);
        outsideCloser.MarkOpened();
        LocalizeTextManager.OnLanguageChanged += Refresh;
        GlobalUiInputSignals.ClickPerformed += HandleCloseCheck;
        GlobalUiInputSignals.EscapePerformed += HandleCloseCheck;
    }

    private void HandleCloseCheck()
    {
        if (panelStack.IsTop(this) && outsideCloser.ShouldClose()) Close();
    }

    public void Open(RegionFacilitySlots target)
    {
        if (region != null) region.OnSlotsChanged -= Refresh;

        buildChoicePanel.Close();
        buildingPanel.gameObject.SetActive(false);
        UnsubscribeOpenOccupant();

        region = target;
        region.OnSlotsChanged += Refresh;

        if (panelReveal != null) panelReveal.Show();
        else gameObject.SetActive(true);
        outsideCloser.MarkOpened();
        Refresh();
        RegionChanged?.Invoke();
    }

    public void Close()
    {
        if (panelReveal != null) panelReveal.Hide();
        else gameObject.SetActive(false);
        buildChoicePanel.gameObject.SetActive(false);
        buildingPanel.gameObject.SetActive(false);
    }

    private void UnsubscribeOpenOccupant()
    {
        if (openOccupant != null)
        {
            openOccupant.Changed -= Refresh;
            openOccupant = null;
        }
    }

    private void OnDisable()
    {
        panelStack.Remove(this);
        LocalizeTextManager.OnLanguageChanged -= Refresh;
        GlobalUiInputSignals.ClickPerformed -= HandleCloseCheck;
        GlobalUiInputSignals.EscapePerformed -= HandleCloseCheck;

        UnsubscribeOpenOccupant();

        if (region != null)
        {
            region.OnSlotsChanged -= Refresh;
            region = null;
        }

        buildChoicePanel.gameObject.SetActive(false);
        buildingPanel.gameObject.SetActive(false);
        RegionChanged?.Invoke();
    }

    private void Refresh()
    {
        if (region == null) return;

        regionNameText.text = region.RegionName;

        for (int i = 0; i < slotViews.Count; i++)
        {
            if (i >= region.Slots.Count)
            {
                slotViews[i].gameObject.SetActive(false);
                continue;
            }

            slotViews[i].gameObject.SetActive(true);
            var slot = region.Slots[i];
            if (slot.IsEmpty)
            {
                slotViews[i].ShowEmpty();
                continue;
            }

            string label = "";
            string level = "";
            string workers = "";
            if (slot.Occupant is ProductionFacility facility)
            {
                label = facility.BasicValue.FacilityDisplayName;
                level = string.Format(DataTableManager.StringTable.Get("Ui_LevelFormat"), facility.UpgradeCount);
                workers = $"{facility.WorkerAmount}/{facility.MaxWorker}";
            }
            else if (slot.Occupant is House house)
            {
                label = house.Config.HouseDisplayName;
                level = string.Format(DataTableManager.StringTable.Get("Ui_LevelFormat"), house.UpgradeCount);
            }
            slotViews[i].ShowBuilt(slot.Icon, label, level, workers);
        }
    }

    private void OnSlotClicked(int index)
    {
        if (TutorialInputGate.BlockPanelOpen) return;
        if (region == null || index >= region.Slots.Count) return;

        var slot = region.Slots[index];
        if (slot.IsEmpty)
        {
            if (buildChoicePanel.gameObject.activeSelf && buildChoicePanel.SlotIndex == index)
            {
                buildChoicePanel.Close();
                return;
            }

            buildingPanel.gameObject.SetActive(false);
            UnsubscribeOpenOccupant();

            buildChoicePanel.Open(region, index);
            return;
        }

        OpenBuiltSlot(index);
    }

    public void OpenBuiltSlot(int index)
    {
        if (region == null || index >= region.Slots.Count) return;

        var slot = region.Slots[index];
        if (slot.Occupant is not IUpgradableOccupant occupant) return;

        buildChoicePanel.Close();

        UnsubscribeOpenOccupant();
        openOccupant = occupant;
        openOccupant.Changed += Refresh;

        buildingPanel.Open(slot.Occupant, region, index);
    }
}
