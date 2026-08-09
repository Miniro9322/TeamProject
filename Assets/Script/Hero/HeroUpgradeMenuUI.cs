using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using VContainer;
using VContainer.Unity;

[Serializable]
public struct ResourceIcon
{
    public ProductionType type;
    public Sprite icon;
}

public class HeroUpgradeMenuUI : MonoBehaviour
{
    private ClickOutsideCloser outsideCloser;
    [SerializeField] private List<Sprite> panelImageList;
    [SerializeField] private List<Sprite> upgradeIconList;

    [SerializeField] private Image banner;
    [SerializeField] private Image upgradeIcon;
    [SerializeField] private GameObject resourcePanel;
    [SerializeField] private HeroUpgradeResourcesUI resourceInfoPrefab;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private List<ResourceIcon> resourceIcons;
    [SerializeField] private TextMeshProUGUI upgradeTierText;
    [SerializeField] private TextMeshProUGUI currentLevelText;

    private HeroUpgradeConfig config;
    private HeroTierUpgradeState upgradeState;
    private ResourcesManager resourcesManager;
    private int tier;
    private Dictionary<ProductionType, HeroUpgradeResourcesUI> resourceInfoMap = new();
    private Dictionary<ProductionType, Sprite> resourceIconMap = new();

    [Inject]
    private void Construct(HeroUpgradeConfig config, HeroTierUpgradeState upgradeState, ResourcesManager resourcesManager)
    {
        this.config = config;
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
            if (upgradeState.TryLevelUp(tier))
            {
                UpdateResourceInfo(upgradeState.GetLevel(tier));
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

    public void Set(int tier)
    {
        this.tier = tier;
        banner.sprite = panelImageList[tier - 1];
        upgradeIcon.sprite = upgradeIconList[tier - 1];
        upgradeTierText.text = $"Upgrade Tier {tier}";
        UpdateResourceInfo(0);
        RefreshUpgradeButton();
    }

    private void RefreshUpgradeButton()
    {
        upgradeButton.interactable = upgradeState.CanLevelUp(tier)
            && resourcesManager.CheckResources(upgradeState.GetNextLevelCost(tier));
    }

    public void UpdateResourceInfo(int currentLevel)
    {
        var entry = config.GetEntry(tier);
        var costs = entry.GetCostForLevel(currentLevel);
        currentLevelText.text = $"LV.{currentLevel}";
        //foreach (var resourceInfo in resourceInfoMap.Values)
        //{
        //    resourceInfo.gameObject.SetActive(false);
        //}
        foreach (var cost in costs)
        {
            if (!resourceInfoMap.TryGetValue(cost.Type, out var resourceInfo))
            {
                resourceInfo = Instantiate(resourceInfoPrefab, resourcePanel.transform);
                //resourceInfo.SetIcon(ResourceIconProvider.GetIcon(cost.Type));
                resourceInfo.SetIcon(resourceIconMap[cost.Type]);
                resourceInfoMap[cost.Type] = resourceInfo;
            }
            resourceInfo.SetAmount(-cost.Amount);
            // resourceInfo.gameObject.SetActive(true);
        }
    }
}
