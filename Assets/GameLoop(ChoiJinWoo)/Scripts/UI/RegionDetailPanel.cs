using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 지역 하나의 상세 패널(사이드 패널) - 구조물 그리드 + 인구 로우 + 이동 버튼.
// 빈 칸 클릭 -> FacilityBuildChoicePanel, 지어진 칸 클릭 -> BuildingPanel(인력/업그레이드).
public class RegionDetailPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI regionNameText;
    [SerializeField] private TextMeshProUGUI populationText;
    [SerializeField] private List<FacilitySlotView> slotViews; // 인스펙터에서 최대 슬롯 개수만큼 미리 배치
    [SerializeField] private FacilityBuildChoicePanel buildChoicePanel;
    [SerializeField] private BuildingPanel buildingPanel;
    [SerializeField] private Button moveButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private CameraRig cameraRig;

    private RegionFacilitySlots region;

    private void Awake()
    {
        for (int i = 0; i < slotViews.Count; i++)
        {
            slotViews[i].SetIndex(i);
            slotViews[i].BindClick(OnSlotClicked);
        }

        if (moveButton != null) moveButton.onClick.AddListener(OnMove);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    public void Open(RegionFacilitySlots target)
    {
        if (region != null) region.OnSlotsChanged -= Refresh;

        region = target;
        region.OnSlotsChanged += Refresh;

        gameObject.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (region != null)
        {
            region.OnSlotsChanged -= Refresh;
            region = null;
        }
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

            string level = "";
            var facility = slot.Occupant.GetComponent<ProductionFacility>();
            if (facility != null) level = $"Lv. {facility.UpgradeCount}";
            slotViews[i].ShowBuilt(slot.Icon, level);
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

        var facility = slot.Occupant.GetComponent<ProductionFacility>();
        if (facility == null) return;

        buildingPanel.gameObject.SetActive(true);
        buildingPanel.InitFacilityInfo(facility);
    }

    private void OnMove()
    {
        if (region == null || cameraRig == null) return;

        var board = region.Module.GetComponent<MapBoard>();
        if (board == null) return;

        Vector3 center = board.WorldBounds.center;
        cameraRig.focus = new Vector3(center.x, cameraRig.focus.y, center.z);
        Close();
    }
}
