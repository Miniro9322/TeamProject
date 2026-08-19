using System;

// 맵 배치가 사라지면서(§기반시설 UI) GameObject/Transform이 필요 없어져 일반 클래스로 전환했다.
// IPlaceAble의 Board/OnBreak/OnResur는 실사용 없는 죽은 구현이었다(Hero만 실제로 씀) - 같이 제거.
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

    // 건설 비용 미리보기/체크용 — 인스턴스 없이도 PreviewConstructCost로 같은 계산을 쓸 수 있다.
    public (ProductionType Type, int Amount)[] GetConstructCost() =>
        basicValue.ConstructProduct.ApplyDiscount(ConstructCostDiscount);

    public event Action OnWorkerChanged;

    // House도 같은 이름의 이벤트를 갖지 않아도 되도록, 인터페이스 쪼는 기존 OnWorkerChanged에 얹는다.
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
    private (ProductionType Type, int Amount)[] upgradeCostCopy = Array.Empty<(ProductionType, int)>();
    private (ProductionType Type, int Amount)[] totalUpgradeSpent = Array.Empty<(ProductionType, int)>();
    private (ProductionType Type, int Amount)[] constructCostPaid = Array.Empty<(ProductionType, int)>();
    public (ProductionType Type, int Amount)[] UpgradeCostCopy => upgradeCostCopy;
    public int UpgradeCount => upgradeCount;

    public string NextUpgradeInfo => nextUpgradeInfo;

    public int MaxUpgrade => maxUpgrade;

    // 실제로 낸 건설·강화 비용을 그대로 읽는다 (세이브 전용 조회)
    public (ProductionType Type, int Amount)[] ConstructCostPaid => constructCostPaid;
    public (ProductionType Type, int Amount)[] TotalUpgradeSpent => totalUpgradeSpent;

    private string nextUpgradeInfo = "자원 생산량 증가";

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

    // 철거: 인력 반납 + 매니저 등록 해제 + 건설 비용/그동안 쓴 업그레이드 비용 환불.
    // GameObject가 없으므로 이걸로 생명주기가 끝난다.
    public void Release()
    {
        ReleaseAllWorkers();
        facilityManager.RemoveFacility(this);

        var construct = constructCostPaid;
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
        else if(upgradeCount % 2 == 1)
        {
            amountUpgrade++;
            productAmount += amountUpgrade * 10;
            nextUpgradeInfo = "시민 배치 수 증가";
        }
        else
        {
            citizenUpgrade++;
            maxWorker = basicValue.DefaultMaxWorker + citizenUpgrade;
            nextUpgradeInfo = "자원 생산량 증가";
        }

        if(upgradeCount == maxUpgrade)
        {
            nextUpgradeInfo = "최대 업그레이드";
        }

        resourcesManager.ProductChanged(upgradeCostCopy);

        // 철거 시 환불할 수 있게 지금까지 업그레이드에 쓴 비용을 누적해둔다.
        for (int i = 0; i < totalUpgradeSpent.Length; i++)
        {
            totalUpgradeSpent[i] = (totalUpgradeSpent[i].Type, totalUpgradeSpent[i].Amount + upgradeCostCopy[i].Amount);
        }

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

    // 세이브 데이터로 건설·강화 상태를 자원 차감 없이 그대로 복원한다 (로드 복원 전용)
    public void RestoreState(
        int savedUpgradeCount,
        int savedWorkerAmount,
        (ProductionType Type, int Amount)[] savedConstructPaid,
        (ProductionType Type, int Amount)[] savedUpgradeSpent)
    {
        // RestoreState는 항상 막 생성한 새 객체에서만 불려서 upgradeCount·amountUpgrade·citizenUpgrade가
        // 이미 C# 기본값 0이다 — 재사용 중인 객체를 리셋하는 경로가 없어 따로 초기화하지 않는다.
        productAmount = basicValue.DefaultAmount + ProductAmountBonus;
        maxWorker = basicValue.DefaultMaxWorker;

        while (upgradeCount < savedUpgradeCount)
        {
            ReplayUpgradeStep();
        }

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

    // Upgrade()의 생산량·최대 인력 규칙만 자원 차감 없이 그대로 재현한다 (RestoreState 전용)
    private void ReplayUpgradeStep()
    {
        upgradeCount++;
        if (upgradeCount % 5 == 0)
        {
            // 원본 Upgrade()도 이 분기는 로그만 남기고 생산량·인력 상태는 안 바꾼다 — 재현할 상태 변화가 없다.
        }
        else if (upgradeCount % 2 == 1)
        {
            amountUpgrade++;
            productAmount += amountUpgrade * 10;
            nextUpgradeInfo = "시민 배치 수 증가";
        }
        else
        {
            citizenUpgrade++;
            maxWorker = basicValue.DefaultMaxWorker + citizenUpgrade;
            nextUpgradeInfo = "자원 생산량 증가";
        }

        if (upgradeCount == maxUpgrade)
        {
            nextUpgradeInfo = "최대 업그레이드";
        }
    }
}
