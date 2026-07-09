using TMPro;
using UnityEngine;

public class BuildingPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI workerText;
    private ProductionFacility facility;

    private void OnDisable()
    {
        facility.OnWorkerChanged -= UpdateWorkerText;
        facility = null;
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

    private void UpdateWorkerText(int cur, int max)
    {
        workerText.text = $"{cur}/{max}";
    }

    public void InitFacilityInfo(ProductionFacility facility)
    {
        this.facility = facility;
        facility.OnWorkerChanged += UpdateWorkerText;
        facility.UpdateWorker();
    }
}
