using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using VContainer;

public class HeroClassUpgradeMenuUI : MonoBehaviour
{
    [SerializeField] private List<Sprite> panelImageList; // index = heroType (0=근거리, 1=원거리)
    [SerializeField] private List<Sprite> upgradeIconList; // index = heroType

    [SerializeField] private Image banner;
    [SerializeField] private Image upgradeIcon;
    [SerializeField] private GameObject resourcePanel;
    [SerializeField] private HeroUpgradeResourcesUI resourceInfoPrefab;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private List<ResourceIcon> resourceIcons;
    [SerializeField] private TextMeshProUGUI upgradeTierText;
    [SerializeField] private TextMeshProUGUI currentLevelText;

    private HeroClassUpgradeState upgradeState;
    private ResourcesManager resourcesManager;
    private int heroType;
    private Dictionary<ProductionType, HeroUpgradeResourcesUI> resourceInfoMap = new();
    private Dictionary<ProductionType, Sprite> resourceIconMap = new();

    [Inject]
    private void Construct(HeroClassUpgradeState upgradeState, ResourcesManager resourcesManager)
    {
        this.upgradeState = upgradeState;
        this.resourcesManager = resourcesManager;
    }

    private void Awake()
    {
        foreach (var resourceIcon in resourceIcons)
        {
            resourceIconMap[resourceIcon.type] = resourceIcon.icon;
        }
        upgradeButton.onClick.AddListener(() =>
        {
            if (upgradeState.TryLevelUp(heroType))
            {
                UpdateResourceInfo(upgradeState.GetLevel(heroType));
                RefreshUpgradeButton();
            }
        });
    }

    private void Start()
    {
        resourcesManager.ProductUpdate += RefreshUpgradeButton;
        RefreshUpgradeButton();
    }

    private void OnDestroy()
    {
        resourcesManager.ProductUpdate -= RefreshUpgradeButton;
        upgradeButton.onClick.RemoveAllListeners();
    }

    public void Set(int heroType)
    {
        this.heroType = heroType;
        banner.sprite = panelImageList[heroType];
        upgradeIcon.sprite = upgradeIconList[heroType];
        upgradeTierText.text = heroType == 0 ? "근거리 업그레이드" : "원거리 업그레이드";
        UpdateResourceInfo(upgradeState.GetLevel(heroType));
        RefreshUpgradeButton();
    }

    private void RefreshUpgradeButton()
    {
        upgradeButton.interactable = upgradeState.CanLevelUp(heroType)
            && resourcesManager.CheckResources(upgradeState.GetNextLevelCost(heroType));
    }

    public void UpdateResourceInfo(int currentLevel)
    {
        var costs = upgradeState.GetCostForLevel(heroType, currentLevel);
        currentLevelText.text = currentLevel >= upgradeState.MaxLevel - 1 ? "Max Level" : $"LV.{currentLevel + 1}";
        foreach (var cost in costs)
        {
            if (!resourceInfoMap.TryGetValue(cost.Type, out var resourceInfo))
            {
                resourceInfo = Instantiate(resourceInfoPrefab, resourcePanel.transform);
                resourceInfo.SetIcon(resourceIconMap[cost.Type]);
                resourceInfoMap[cost.Type] = resourceInfo;
            }
            resourceInfo.SetAmount(-cost.Amount);
        }
    }
}
