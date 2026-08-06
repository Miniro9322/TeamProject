using System;

// 맵 배치가 사라지면서 GameObject/Transform이 필요 없어져 일반 클래스로 전환했다.
// 예전 Start()/OnDestroy()가 하던 일을 Init()/Release()로 명시적으로 호출한다.
// 강화(Upgrade) 로직은 ProductionFacility.Upgrade()/CheckCanUpgrade()와 같은 패턴을 그대로 옮겨온 것.
public class House : IUpgradableOccupant
{
    private readonly HouseConfig config;
    private readonly CitizenManager citizenManager;
    private readonly ResourcesManager resourcesManager;

    private int maxUpgrade = 4;
    private int upgradeCount = 0;
    private (ProductionType Type, int Amount)[] upgradeCostCopy = Array.Empty<(ProductionType, int)>();
    private (ProductionType Type, int Amount)[] totalUpgradeSpent = Array.Empty<(ProductionType, int)>();

    public string HouseName => config.HouseName;
    public string HouseInfo => config.HouseInfo;
    public (ProductionType Type, int Amount)[] Resources => config.Resources;
    public int UpgradeCount => upgradeCount;
    public (ProductionType Type, int Amount)[] UpgradeCostCopy => upgradeCostCopy;
    private string nextUpgradeInfo = "최대 시민 수 증가";
    public string NextUpgradeInfo => nextUpgradeInfo;

    public int MaxUpgrade => maxUpgrade;

    public event Action Changed;

    public House(HouseConfig config, CitizenManager citizenManager, ResourcesManager resourcesManager)
    {
        this.config = config;
        this.citizenManager = citizenManager;
        this.resourcesManager = resourcesManager;
    }

    public void Init()
    {
        citizenManager.IncreaseMaxCitizen(config.MaxCitizenAmount);
        resourcesManager.ProductChanged(Resources);

        upgradeCostCopy = config.UpgradeCost;
        totalUpgradeSpent = new (ProductionType, int)[upgradeCostCopy.Length];
        for (int i = 0; i < totalUpgradeSpent.Length; i++)
        {
            totalUpgradeSpent[i] = (upgradeCostCopy[i].Type, 0);
        }
    }

    // 철거: 건설 비용 + 그동안 강화에 쓴 비용을 환불한다(ProductionFacility.Release()와 동일 패턴).
    public void Release()
    {
        var resources = Resources;
        var refund = new (ProductionType Type, int Amount)[resources.Length + totalUpgradeSpent.Length];
        for (int i = 0; i < resources.Length; i++)
        {
            refund[i] = (resources[i].Type, -resources[i].Amount);
        }
        for (int i = 0; i < totalUpgradeSpent.Length; i++)
        {
            refund[resources.Length + i] = (totalUpgradeSpent[i].Type, -totalUpgradeSpent[i].Amount);
        }
        resourcesManager.ProductChanged(refund);
    }

    public void Upgrade()
    {
        upgradeCount++;
        citizenManager.IncreaseMaxCitizen(config.CitizenPerUpgrade);

        resourcesManager.ProductChanged(upgradeCostCopy);

        for (int i = 0; i < totalUpgradeSpent.Length; i++)
        {
            totalUpgradeSpent[i] = (totalUpgradeSpent[i].Type, totalUpgradeSpent[i].Amount + upgradeCostCopy[i].Amount);
        }

        var baseCost = config.UpgradeCost;
        for (int i = 0; i < upgradeCostCopy.Length; i++)
        {
            upgradeCostCopy[i] = (baseCost[i].Type, baseCost[i].Amount * (upgradeCount + 1));
        }

        Changed?.Invoke();
    }

    public bool CheckCanUpgrade()
    {
        return upgradeCount < maxUpgrade && resourcesManager.CheckResources(upgradeCostCopy);
    }
}
