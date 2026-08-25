using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

// 지역 하나의 상세 패널(사이드 패널) - 구조물 그리드 + 인구 로우.
// 빈 칸 클릭 -> FacilityBuildChoicePanel, 지어진 칸 클릭 -> BuildingPanel(인력/업그레이드).
public class RegionDetailPanel : MonoBehaviour, IClosablePanel
{
    [SerializeField] private TextMeshProUGUI regionNameText;
    [SerializeField] private List<FacilitySlotView> slotViews; // 인스펙터에서 최대 슬롯 개수만큼 미리 배치
    [SerializeField] private FacilityBuildChoicePanel buildChoicePanel;
    [SerializeField] private BuildingPanel buildingPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private RegionOverviewPanel overviewPanel; // 지역 노드 버튼 클릭은 "바깥 클릭"이 아니다

    private RegionFacilitySlots region;
    private IUpgradableOccupant openOccupant;
    private UiPanelStack panelStack;
    private ClickOutsideCloser outsideCloser;

    // 지금 열려서 보여주고 있는 지역 - 같은 지역 노드를 다시 눌렀는지 오버뷰가 판단하는 데 쓴다.
    public RegionFacilitySlots CurrentRegion => gameObject.activeSelf ? region : null;

    [Inject]
    private void Construct(UiPanelStack panelStack)
    {
        this.panelStack = panelStack;
    }

    private void Awake()
    {
        for (int i = 0; i < slotViews.Count; i++)
        {
            slotViews[i].SetIndex(i);
            slotViews[i].BindClick(OnSlotClicked);
        }

        if (closeButton != null) closeButton.onClick.AddListener(Close);
        buildChoicePanel.gameObject.SetActive(false);
        buildingPanel.gameObject.SetActive(false);

        Transform[] nodeTransforms = null;
        if (overviewPanel != null)
        {
            var nodes = overviewPanel.Nodes;
            nodeTransforms = new Transform[nodes.Count];
            for (int i = 0; i < nodes.Count; i++) nodeTransforms[i] = nodes[i].transform;
        }
        outsideCloser = new ClickOutsideCloser((RectTransform)transform, nodeTransforms);
    }

    private void OnEnable()
    {
        panelStack.Push(this);
        outsideCloser.MarkOpened();
        LocalizeTextManager.OnLanguageChanged += Refresh;
    }

    // buildChoicePanel/buildingPanel도 같은 UiPanelStack에 Push되는 패널이라, 둘 중 하나가 열리면
    // 그게 스택 맨 위가 되어 IsTop(this)가 자연히 false가 된다 - 그쪽 Update()가 먼저 처리하고 여긴 쉰다.
    private void Update()
    {
        if (panelStack.IsTop(this) && outsideCloser.ShouldClose()) Close();
    }

    public void Open(RegionFacilitySlots target)
    {
        if (region != null) region.OnSlotsChanged -= Refresh;

        // 다른 지역으로 옮겨가는 거라, 이전 지역 슬롯에 물려있던 팝업은 정리한다.
        buildChoicePanel.Close();
        buildingPanel.gameObject.SetActive(false);
        UnsubscribeOpenOccupant();

        region = target;
        region.OnSlotsChanged += Refresh;

        gameObject.SetActive(true);
        outsideCloser.MarkOpened();
        Refresh();
    }

    public void Close()
    {
        gameObject.SetActive(false);
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

        UnsubscribeOpenOccupant();

        if (region != null)
        {
            region.OnSlotsChanged -= Refresh;
            region = null;
        }

        buildChoicePanel.gameObject.SetActive(false);
        buildingPanel.gameObject.SetActive(false);
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
        if (region == null || index >= region.Slots.Count) return;

        var slot = region.Slots[index];
        if (slot.IsEmpty)
        {
            buildingPanel.gameObject.SetActive(false); // 지어진 칸용 패널이 열려있었다면 정리
            UnsubscribeOpenOccupant();

            buildChoicePanel.Open(region, index);
            return;
        }

        if (slot.Occupant is not IUpgradableOccupant occupant) return;

        buildChoicePanel.Close(); // 빈 칸용 패널이 열려있었다면 정리

        UnsubscribeOpenOccupant();
        openOccupant = occupant;
        openOccupant.Changed += Refresh;

        buildingPanel.gameObject.SetActive(true);
        buildingPanel.InitOccupant(slot.Occupant, region, index);
    }
}
