using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildFacilityPanel : MonoBehaviour
{
    [SerializeField] private MapView view;
    [SerializeField] private MapGame game;
    [SerializeField] private Button foodFacility;
    [SerializeField] private Button goldFacility;
    [SerializeField] private Button stoneFacility;
    [SerializeField] private Button woodFacility;
    [SerializeField] private Button ironFacility;
    [SerializeField] private Button house;
    [SerializeField] private GameObject facilityInfoPanel;
    [SerializeField] private Image facilityIcon;
    [SerializeField] private TextMeshProUGUI facilityInfoText;
    private string currentLabel;

    private void OnEnable()
    {
        game.Placer.resourcesManager.ProductUpdate += ButtonUpdate;
        facilityInfoPanel.SetActive(false);
        ButtonUpdate();
    }

    private void OnDisable()
    {
        game.Placer.resourcesManager.ProductUpdate -= ButtonUpdate;
    }

    public void OnFacility(string label)
    {
        currentLabel = label;
        var temp = view.GetSlot(label).prefab.GetComponent<ProductionFacility>();
        if(temp != null)
        {
            facilityIcon.sprite = view.GetSlot(label).icon;
            var sb = new StringBuilder();
            sb.Append($"{temp.BasicValue.FacilityName}\n{temp.BasicValue.FacilityInfo}\n생산 자원: {temp.ProductionType}\n건설 소모 자원\n");
            foreach (var item in temp.GetConstructCost())
            {
                sb.Append($"{item.Type}: {-item.Amount} ");
            }
            facilityInfoText.text = sb.ToString().Trim();
            facilityInfoPanel.SetActive(true);
        }
        else
        {
            var house = view.GetSlot(label).prefab.GetComponent<House>();
            facilityIcon.sprite = view.GetSlot(label).icon;
            var sb = new StringBuilder();
            sb.Append($"{house.HouseName}\n{house.HouseInfo}\n건설 소모 자원\n");
            foreach (var item in house.Resources)
            {
                sb.Append($"{item.Type}: {-item.Amount} ");
            }
            facilityInfoText.text = sb.ToString().Trim();
            facilityInfoPanel.SetActive(true);
        }
    }

    public void OnBuild()
    {
        view.SetUnit(currentLabel);
        facilityInfoPanel.SetActive(false);
    }

    private void ButtonUpdate()
    {
        foodFacility.interactable = view.CheckCanBuild("Food");
        goldFacility.interactable = view.CheckCanBuild("Gold");
        stoneFacility.interactable = view.CheckCanBuild("Stone");
        woodFacility.interactable = view.CheckCanBuild("Wood");
        ironFacility.interactable = view.CheckCanBuild("Iron");
        house.interactable = view.CheckCanBuild("House");

        if (view.IsPlacing && !view.CheckCanBuild(view.PlacingLabel))
            view.ClearMode();
    }
}
