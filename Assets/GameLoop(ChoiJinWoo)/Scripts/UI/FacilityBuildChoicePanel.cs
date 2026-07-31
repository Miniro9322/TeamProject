using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

// 빈 슬롯에 지을 건물 종류를 고르는 팝업. BuildFacilityPanel의 비용 미리보기 로직을 그대로 가져왔다
// (맵 배치 대신 슬롯 배치로 바뀐 것 말고는 흐름이 동일하다).
public class FacilityBuildChoicePanel : MonoBehaviour
{
    [SerializeField] private PlacePalette palette;
    [SerializeField] private List<Button> optionButtons;   // optionLabels와 인덱스가 대응
    [SerializeField] private List<string> optionLabels;     // palette 슬롯의 label과 일치해야 함
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private Image infoIcon;
    [SerializeField] private TextMeshProUGUI infoText;

    private BaseConstructor constructor;
    private ResourcesManager resourcesManager;
    private RegionFacilitySlots region;
    private int slotIndex;
    private string currentLabel;

    [Inject]
    private void Construct(BaseConstructor constructor, ResourcesManager resourcesManager)
    {
        this.constructor = constructor;
        this.resourcesManager = resourcesManager;
    }

    private void OnEnable()
    {
        resourcesManager.ProductUpdate += RefreshButtons;
        infoPanel.SetActive(false);
        RefreshButtons();
    }

    private void OnDisable()
    {
        resourcesManager.ProductUpdate -= RefreshButtons;
    }

    public void Open(RegionFacilitySlots target, int index)
    {
        region = target;
        slotIndex = index;
        gameObject.SetActive(true);
    }

    private void RefreshButtons()
    {
        for (int i = 0; i < optionButtons.Count && i < optionLabels.Count; i++)
        {
            var slot = palette.GetSlot(optionLabels[i]);
            optionButtons[i].interactable = slot != null && constructor.CanBuild(slot);
        }
    }

    public void OnOption(string label)
    {
        currentLabel = label;
        var slot = palette.GetSlot(label);
        if (slot == null) return;

        infoIcon.sprite = slot.icon;
        var sb = new StringBuilder();
        var facility = slot.prefab.GetComponent<ProductionFacility>();
        if (facility != null)
        {
            sb.Append($"{facility.BasicValue.FacilityName}\n{facility.BasicValue.FacilityInfo}\n생산 자원: {facility.ProductionType}\n건설 소모 자원\n");
            foreach (var cost in facility.GetConstructCost())
            {
                sb.Append($"{cost.Type}: {-cost.Amount} ");
            }
        }
        else
        {
            var house = slot.prefab.GetComponent<House>();
            sb.Append($"{house.HouseName}\n{house.HouseInfo}\n건설 소모 자원\n");
            foreach (var cost in house.Resources)
            {
                sb.Append($"{cost.Type}: {-cost.Amount} ");
            }
        }
        infoText.text = sb.ToString().Trim();
        infoPanel.SetActive(true);
    }

    public void OnBuild()
    {
        var slot = palette.GetSlot(currentLabel);
        if (slot == null || region == null) return;

        if (constructor.TryBuild(slot, region, slotIndex, out _))
        {
            infoPanel.SetActive(false);
            gameObject.SetActive(false);
        }
    }
}
