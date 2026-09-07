using System;

public class ProductionFacility : IUpgradableOccupant
{
    private readonly ProductionValue basicValue;
    private readonly ProductionEconomyConfig economyConfig;
    private readonly ResourcesManager resourcesManager;
    private readonly CitizenManager citizenManager;
    private readonly FacilityManager facilityManager;
    private readonly UpgradeState upgradeState;

    private int productAmount;
    private int workerAmount = 0;
    private int maxWorker;
    public ProductionType ProductionType => basicValue.Type;

    private float ConstructCostDiscount => upgradeState.GetTotalEffect(economyConfig.ConstructCostUpgrades);
    private float UpgradeCostDiscount => upgradeState.GetTotalEffect(economyConfig.UpgradeCostUpgrades);
    private int ProductAmountBonus => (int)upgradeState.GetTotalEffect(economyConfig.ProductAmountUpgrades);

    public (ProductionType Type, int Amount)[] GetConstructCost() =>
        basicValue.ConstructProduct.ApplyDiscount(ConstructCostDiscount);

    public event Action OnWorkerChanged;

    event Action IUpgradableOccupant.Changed
    {
        add => OnWorkerChanged += value;
        remove => OnWorkerChanged -= value;
    }

    public ProductionValue BasicValue => basicValue;

    private int maxUpgrade = 4;
    private int upgradeCount = 0;
    private int amountUpgrade = 0;
    private int citizenUpgrade = 0;
    public int WorkerAmount => workerAmount;
    public int MaxWorker => maxWorker;
    public int ProductAmount => productAmount;
    public int AmountUpgrade => amountUpgrade;
    public int CitizenUpgrade => citizenUpgrade;
    private (ProductionType Type, int Amount)[] upgradeCostCopy = Array.Empty<(ProductionType, int)>();
    private (ProductionType Type, int Amount)[] totalUpgradeSpent = Array.Empty<(ProductionType, int)>();
    private (ProductionType Type, int Amount)[] constructCostPaid = Array.Empty<(ProductionType, int)>();
    public (ProductionType Type, int Amount)[] UpgradeCostCopy => upgradeCostCopy;
    public int UpgradeCount => upgradeCount;

    public string NextUpgradeInfo => nextUpgradeInfo;

    public int MaxUpgrade => maxUpgrade;

    public (ProductionType Type, int Amount)[] ConstructCostPaid => constructCostPaid;
    public (ProductionType Type, int Amount)[] TotalUpgradeSpent => totalUpgradeSpent;

    private string nextUpgradeInfo = "Ui_UpgradeInfo_ProductAmount";

    public ProductionFacility(
        ProductionValue basicValue,
        ProductionEconomyConfig economyConfig,
        ResourcesManager resourcesManager,
        CitizenManager citizenManager,
        FacilityManager facilityManager,
        UpgradeState upgradeState)
    {
        this.basicValue = basicValue;
        this.economyConfig = economyConfig;
        this.resourcesManager = resourcesManager;
        this.citizenManager = citizenManager;
        this.facilityManager = facilityManager;
        this.upgradeState = upgradeState;
    }

    public static (ProductionType Type, int Amount)[] PreviewConstructCost(
        ProductionValue basicValue, ProductionEconomyConfig economyConfig, UpgradeState upgradeState)
    {
        float discount = upgradeState.GetTotalEffect(economyConfig.ConstructCostUpgrades);
        return basicValue.ConstructProduct.ApplyDiscount(discount);
    }

    public void Init()
    {
        productAmount = basicValue.DefaultAmount + amountUpgrade * 10 + ProductAmountBonus;
        maxWorker = basicValue.DefaultMaxWorker + citizenUpgrade;
        workerAmount = 0;
        upgradeCostCopy = BasicValue.UpgradeCost.ApplyDiscount(UpgradeCostDiscount);
        totalUpgradeSpent = new (ProductionType, int)[upgradeCostCopy.Length];
        for (int i = 0; i < totalUpgradeSpent.Length; i++)
        {
            totalUpgradeSpent[i] = (upgradeCostCopy[i].Type, 0);
        }

        constructCostPaid = GetConstructCost();
        resourcesManager.ProductChanged(constructCostPaid);
        facilityManager.AddFacility(this);
    }

    private void ReleaseAllWorkers()
    {
        while (workerAmount > 0)
        {
            workerAmount--;
            citizenManager.RecycleCitizen();
        }
        UpdateInfo();
    }

    public void Release()
    {
        ReleaseAllWorkers();
        facilityManager.RemoveFacility(this);

        resourcesManager.ProductChanged(constructCostPaid.BuildRefundWith(totalUpgradeSpent));
    }

    public void IncreaseWorker()
    {
        if (citizenManager == null) return;

        if (workerAmount < maxWorker && citizenManager.CheckCanUseCitizen())
        {
            workerAmount++;
            UpdateInfo();
            citizenManager.UseCitizen();
        }
    }

    public void DecreaseWorker()
    {
        if (citizenManager == null) return;

        if (workerAmount > 0)
        {
            workerAmount--;
            UpdateInfo();
            citizenManager.RecycleCitizen();
        }
    }

    public void UpdateInfo()
    {
        OnWorkerChanged?.Invoke();
    }

    public (ProductionType, int) ProduceProduction()
    {
        return (basicValue.Type, productAmount * workerAmount);
    }

    public void Upgrade()
    {
        upgradeCount++;
        
        if(upgradeCount % 2 == 1)
        {
            amountUpgrade++;
            productAmount += amountUpgrade * 10;
            nextUpgradeInfo = "Ui_UpgradeInfo_Worker";
        }
        else
        {
            citizenUpgrade++;
            maxWorker = basicValue.DefaultMaxWorker + citizenUpgrade;
            nextUpgradeInfo = "Ui_UpgradeInfo_ProductAmount";
        }

        if(upgradeCount == maxUpgrade)
        {
            nextUpgradeInfo = "Ui_UpgradeInfo_MaxUpgrade";
        }

        resourcesManager.ProductChanged(upgradeCostCopy);

        upgradeCostCopy.AccumulateInto(totalUpgradeSpent);

        var baseCost = basicValue.UpgradeCost.ApplyDiscount(UpgradeCostDiscount);
        for (int i = 0; i < upgradeCostCopy.Length; i++)
        {
            upgradeCostCopy[i] = (baseCost[i].Type, baseCost[i].Amount * (upgradeCount + 1));
        }

        UpdateInfo();
    }

    public bool CheckCanUpgrade()
    {
        return upgradeCount < maxUpgrade && resourcesManager.CheckResources(upgradeCostCopy);
    }

    public void RestoreState(
        int savedUpgradeCount,
        int savedWorkerAmount,
        int savedProductAmount,
        int savedMaxWorker,
        int savedAmountUpgrade,
        int savedCitizenUpgrade,
        string savedNextUpgradeInfo,
        (ProductionType Type, int Amount)[] savedConstructPaid,
        (ProductionType Type, int Amount)[] savedUpgradeSpent)
    {
        upgradeCount = savedUpgradeCount;
        productAmount = savedProductAmount;
        maxWorker = savedMaxWorker;
        amountUpgrade = savedAmountUpgrade;
        citizenUpgrade = savedCitizenUpgrade;
        nextUpgradeInfo = savedNextUpgradeInfo;
        workerAmount = savedWorkerAmount;
        constructCostPaid = savedConstructPaid;
        totalUpgradeSpent = savedUpgradeSpent;

        var baseCost = basicValue.UpgradeCost.ApplyDiscount(UpgradeCostDiscount);
        upgradeCostCopy = new (ProductionType, int)[baseCost.Length];
        for (int i = 0; i < upgradeCostCopy.Length; i++)
        {
            upgradeCostCopy[i] = (baseCost[i].Type, baseCost[i].Amount * (upgradeCount + 1));
        }

        facilityManager.AddFacility(this);
        UpdateInfo();
    }
}
