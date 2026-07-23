using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildingPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI workerText;
    [SerializeField] private TextMeshProUGUI perProductText;
    [SerializeField] private TextMeshProUGUI upgradeCostText;
    [SerializeField] private TextMeshProUGUI FacilityLevelText;
    [SerializeField] private Button upgradeButton;
    private ProductionFacility facility;

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
        workerText.text = $"{facility.WorkerAmount}/{facility.MaxWorker}";
        upgradeButton.interactable = facility.CheckCanUpgrade();
        perProductText.text = $"{facility.ProductAmount * facility.WorkerAmount}/day";
        var sb = new StringBuilder();
        foreach(var cost in facility.UpgradeCostCopy)
        {
            sb.Append($"{cost.Key} : {-cost.Value}\n");
        }
        sb.Length--;
        upgradeCostText.text = sb.ToString();
        FacilityLevelText.text = $"Lv. {facility.UpgradeCount - 1}";
    }

    public void InitFacilityInfo(ProductionFacility facility)
    {
        this.facility = facility;
        facility.OnWorkerChanged += UpdatePanel;
        UpdatePanel();
    }

    private void OnDisable()
    {
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
}
