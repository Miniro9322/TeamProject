using System;

// 맵 배치가 사라지면서(§기반시설 UI) GameObject/Transform이 필요 없어져 일반 클래스로 전환했다.
// IPlaceAble의 Board/OnBreak/OnResur는 실사용 없는 죽은 구현이었다(Hero만 실제로 씀) - 같이 제거.
public class ProductionFacility
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

    // 건설 비용 미리보기/체크용 — 인스턴스 없이도 PreviewConstructCost로 같은 계산을 쓸 수 있다.
    public (ProductionType Type, int Amount)[] GetConstructCost() =>
        ApplyDiscount(basicValue.ConstructProduct, ConstructCostDiscount);

    public event Action OnWorkerChanged;

    public ProductionValue BasicValue => basicValue;

    private int maxUpgrade = 4;
    private int upgradeCount = 0;
    private int amountUpgrade = 0;
    private int citizenUpgrade = 0;
    public int WorkerAmount => workerAmount;
    public int MaxWorker => maxWorker;
    public int ProductAmount => productAmount;
    private (ProductionType Type, int Amount)[] upgradeCostCopy = Array.Empty<(ProductionType, int)>();
    private (ProductionType Type, int Amount)[] totalUpgradeSpent = Array.Empty<(ProductionType, int)>();
    public (ProductionType Type, int Amount)[] UpgradeCostCopy => upgradeCostCopy;
    public int UpgradeCount => upgradeCount;

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

    // 인스턴스를 만들지 않고도(건설 전 미리보기) 같은 할인 공식으로 건설 비용을 계산한다.
    public static (ProductionType Type, int Amount)[] PreviewConstructCost(
        ProductionValue basicValue, ProductionEconomyConfig economyConfig, UpgradeState upgradeState)
    {
        float discount = upgradeState.GetTotalEffect(economyConfig.ConstructCostUpgrades);
        return ApplyDiscount(basicValue.ConstructProduct, discount);
    }

    private static (ProductionType Type, int Amount)[] ApplyDiscount((ProductionType Type, int Amount)[] cost, float discount)
    {
        var result = new (ProductionType, int)[cost.Length];
        for (int i = 0; i < cost.Length; i++)
            result[i] = (cost[i].Type, UnityEngine.Mathf.RoundToInt(cost[i].Amount * (1f - discount)));
        return result;
    }

    public void Init()
    {
        productAmount = basicValue.DefaultAmount + amountUpgrade * 10 + ProductAmountBonus;
        maxWorker = basicValue.DefaultMaxWorker + citizenUpgrade;
        workerAmount = 0;
        upgradeCostCopy = ApplyDiscount(BasicValue.UpgradeCost, UpgradeCostDiscount);
        totalUpgradeSpent = new (ProductionType, int)[upgradeCostCopy.Length];
        for (int i = 0; i < totalUpgradeSpent.Length; i++)
        {
            totalUpgradeSpent[i] = (upgradeCostCopy[i].Type, 0);
        }

        resourcesManager.ProductChanged(GetConstructCost());
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

    // 철거: 인력 반납 + 매니저 등록 해제 + 건설 비용/그동안 쓴 업그레이드 비용 환불.
    // GameObject가 없으므로 이걸로 생명주기가 끝난다.
    public void Release()
    {
        ReleaseAllWorkers();
        facilityManager.RemoveFacility(this);

        var construct = basicValue.ConstructProduct;
        var refund = new (ProductionType Type, int Amount)[construct.Length + totalUpgradeSpent.Length];
        for (int i = 0; i < construct.Length; i++)
        {
            refund[i] = (construct[i].Type, -construct[i].Amount);
        }
        for (int i = 0; i < totalUpgradeSpent.Length; i++)
        {
            refund[construct.Length + i] = (totalUpgradeSpent[i].Type, -totalUpgradeSpent[i].Amount);
        }
        resourcesManager.ProductChanged(refund);
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
        if(upgradeCount % 5 == 0)
        {
            UnityEngine.Debug.Log("특수 자원 생산 시작");
        }
        else if(upgradeCount % 2 == 0)
        {
            amountUpgrade++;
            productAmount += amountUpgrade * 10;
        }
        else
        {
            citizenUpgrade++;
            maxWorker = basicValue.DefaultMaxWorker + citizenUpgrade;
        }

        resourcesManager.ProductChanged(upgradeCostCopy);

        // 철거 시 환불할 수 있게 지금까지 업그레이드에 쓴 비용을 누적해둔다.
        for (int i = 0; i < totalUpgradeSpent.Length; i++)
        {
            totalUpgradeSpent[i] = (totalUpgradeSpent[i].Type, totalUpgradeSpent[i].Amount + upgradeCostCopy[i].Amount);
        }

        var baseCost = ApplyDiscount(basicValue.UpgradeCost, UpgradeCostDiscount);
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
}
