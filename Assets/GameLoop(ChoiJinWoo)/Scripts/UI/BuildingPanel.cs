using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BuildingPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI workerText;
    [SerializeField] private TextMeshProUGUI perProductText;
    [SerializeField] private TextMeshProUGUI upgradeCostText;
    [SerializeField] private TextMeshProUGUI FacilityLevelText;
    [SerializeField] private Button upgradeButton;
    private ProductionFacility facility;
    private RectTransform rectTransform;
    private int openedFrame;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
    }

    private void OnEnable()
    {
        openedFrame = Time.frameCount;
    }

    private void Update()
    {
        if (Time.frameCount == openedFrame) return; // 패널이 열린 바로 그 프레임의 클릭은 무시

        if (Mouse.current.leftButton.wasPressedThisFrame &&
            !RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Mouse.current.position.ReadValue(), null))
        {
            gameObject.SetActive(false);
        }
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
