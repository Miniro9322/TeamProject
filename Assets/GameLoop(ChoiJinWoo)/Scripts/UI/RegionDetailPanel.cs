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
    private ProductionFacility openFacility;
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
        // 지역 노드 버튼(RegionOverviewPanel 소속)을 눌러 다른 지역으로 옮겨갈 때, 그 클릭이 "바깥
        // 클릭"으로 잡혀 Close()가 먼저 불리고 곧이어 Open()이 다시 켜는 깜빡임을 막는다.
        // 오버뷰 전체가 아니라 노드 버튼들만 예외로 둔다 - 오버뷰는 화면 전체를 덮고 있어서 전체를
        // 예외로 두면 진짜 바깥 클릭(닫기)까지 다 막혀버린다.
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
    }

    // buildChoicePanel/buildingPanel은 하이러키상 자식이 아니라 필드로만 참조되는 별도 패널이라,
    // 그 안의 버튼을 눌러도 이 패널 입장에선 "바깥 클릭"으로 보인다. 둘 중 하나라도 열려있으면
    // 바깥 클릭 판정은 그쪽(자기 자신 하위 트리는 자기가 챙김)에 맡기고 여기선 쉰다.
    private void Update()
    {
        if (buildChoicePanel.gameObject.activeSelf || buildingPanel.gameObject.activeSelf) return;
        if (outsideCloser.ClickedOutside()) Close();
    }

    public void Open(RegionFacilitySlots target)
    {
        if (region != null) region.OnSlotsChanged -= Refresh;

        // 다른 지역으로 옮겨가는 거라, 이전 지역 슬롯에 물려있던 팝업은 정리한다.
        buildChoicePanel.Close();
        buildingPanel.gameObject.SetActive(false);
        if (openFacility != null)
        {
            openFacility.OnWorkerChanged -= Refresh;
            openFacility = null;
        }

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

    private void OnDisable()
    {
        panelStack.Remove(this);

        if (openFacility != null)
        {
            openFacility.OnWorkerChanged -= Refresh;
            openFacility = null;
        }

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

            string level = "";
            string workers = "";
            if (slot.Occupant is ProductionFacility facility)
            {
                level = $"Lv.{facility.UpgradeCount}";
                workers = $"{facility.WorkerAmount}/{facility.MaxWorker}";
            }
            slotViews[i].ShowBuilt(slot.Icon, slot.Label, level, workers);
        }
    }

    private void OnSlotClicked(int index)
    {
        if (region == null || index >= region.Slots.Count) return;

        var slot = region.Slots[index];
        if (slot.IsEmpty)
        {
            buildingPanel.gameObject.SetActive(false); // 지어진 칸용 패널이 열려있었다면 정리
            if (openFacility != null)
            {
                openFacility.OnWorkerChanged -= Refresh;
                openFacility = null;
            }

            buildChoicePanel.Open(region, index);
            return;
        }

        if (slot.Occupant is not ProductionFacility facility) return;

        buildChoicePanel.Close(); // 빈 칸용 패널이 열려있었다면 정리

        if (openFacility != null) openFacility.OnWorkerChanged -= Refresh;
        openFacility = facility;
        openFacility.OnWorkerChanged += Refresh;

        buildingPanel.gameObject.SetActive(true);
        buildingPanel.InitFacilityInfo(facility, region, index);
    }
}
