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
    [SerializeField] private TextMeshProUGUI populationText;
    [SerializeField] private List<FacilitySlotView> slotViews; // 인스펙터에서 최대 슬롯 개수만큼 미리 배치
    [SerializeField] private FacilityBuildChoicePanel buildChoicePanel;
    [SerializeField] private BuildingPanel buildingPanel;
    [SerializeField] private Button closeButton;

    private RegionFacilitySlots region;
    private ProductionFacility openFacility;
    private UiPanelStack panelStack;
    private ClickOutsideCloser outsideCloser;

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
        outsideCloser = new ClickOutsideCloser((RectTransform)transform);
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
        populationText.text = $"{region.TotalWorkers()}";

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

            slotViews[i].ShowBuilt(slot.Icon, slot.Label);
        }
    }

    private void OnSlotClicked(int index)
    {
        if (region == null || index >= region.Slots.Count) return;

        var slot = region.Slots[index];
        if (slot.IsEmpty)
        {
            buildChoicePanel.Open(region, index);
            return;
        }

        if (slot.Occupant is not ProductionFacility facility) return;

        if (openFacility != null) openFacility.OnWorkerChanged -= Refresh;
        openFacility = facility;
        openFacility.OnWorkerChanged += Refresh;

        buildingPanel.gameObject.SetActive(true);
        buildingPanel.InitFacilityInfo(facility, region, index);
    }
}
