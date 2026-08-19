// 기반시설 UI에서 곧장 건물을 짓는 담당. 맵 배치가 없고(PlacementArea/MapBoard 불필요),
// ProductionFacility/House가 이제 POCO라 풀링/프리팹 인스턴스화도 필요 없다 - BuildableFacility의
// 설정 SO(facilityValue/houseConfig)로 곧장 만든다.
public class BaseConstructor
{
    private readonly ResourcesManager resourcesManager;
    private readonly CitizenManager citizenManager;
    private readonly FacilityManager facilityManager;
    private readonly UpgradeState upgradeState;
    private readonly ProductionEconomyConfig economyConfig;

    public BaseConstructor(
        ResourcesManager resourcesManager,
        CitizenManager citizenManager,
        FacilityManager facilityManager,
        UpgradeState upgradeState,
        ProductionEconomyConfig economyConfig)
    {
        this.resourcesManager = resourcesManager;
        this.citizenManager = citizenManager;
        this.facilityManager = facilityManager;
        this.upgradeState = upgradeState;
        this.economyConfig = economyConfig;
    }

    public bool CanBuild(BuildableFacility option)
    {
        switch (option.kind)
        {
            case OccupantKind.Resource:
                return option.facilityValue != null &&
                    resourcesManager.CheckResources(ProductionFacility.PreviewConstructCost(option.facilityValue, economyConfig, upgradeState));
            case OccupantKind.Building:
                return option.houseConfig != null &&
                    resourcesManager.CheckResources(House.PreviewConstructCost(option.houseConfig, economyConfig, upgradeState));
            default:
                return false;
        }
    }

    public bool TryBuild(BuildableFacility option, RegionFacilitySlots region, int slotIndex, out object built)
    {
        built = null;
        if (slotIndex < 0 || slotIndex >= region.Slots.Count || !region.Slots[slotIndex].IsEmpty) return false;
        if (!CanBuild(option)) return false;

        built = option.kind == OccupantKind.Resource ? BuildFacility(option) : BuildHouse(option);
        if (built == null) return false;

        region.TryAssign(slotIndex, built, option.icon, option.DisplayName);
        return true;
    }

    private ProductionFacility BuildFacility(BuildableFacility option)
    {
        var facility = new ProductionFacility(option.facilityValue, economyConfig, resourcesManager, citizenManager, facilityManager, upgradeState);
        facility.Init();
        return facility;
    }

    private House BuildHouse(BuildableFacility option)
    {
        var house = new House(option.houseConfig, citizenManager, resourcesManager, upgradeState, economyConfig);
        house.Init();
        return house;
    }

    // 세이브 데이터로 기반시설을 자원 차감 없이 복원해 슬롯에 채운다 (로드 복원 전용)
    public object RestoreBuild(
        BuildableFacility option,
        RegionFacilitySlots region,
        int slotIndex,
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
        object built = option.kind == OccupantKind.Resource
            ? RestoreFacility(option, savedUpgradeCount, savedWorkerAmount, savedProductAmount, savedMaxWorker, savedAmountUpgrade, savedCitizenUpgrade, savedNextUpgradeInfo, savedConstructPaid, savedUpgradeSpent)
            : RestoreHouse(option, savedUpgradeCount, savedConstructPaid, savedUpgradeSpent);

        region.TryAssign(slotIndex, built, option.icon, option.DisplayName);
        return built;
    }

    private ProductionFacility RestoreFacility(
        BuildableFacility option,
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
        var facility = new ProductionFacility(option.facilityValue, economyConfig, resourcesManager, citizenManager, facilityManager, upgradeState);
        facility.RestoreState(savedUpgradeCount, savedWorkerAmount, savedProductAmount, savedMaxWorker, savedAmountUpgrade, savedCitizenUpgrade, savedNextUpgradeInfo, savedConstructPaid, savedUpgradeSpent);
        return facility;
    }

    private House RestoreHouse(
        BuildableFacility option,
        int savedUpgradeCount,
        (ProductionType Type, int Amount)[] savedConstructPaid,
        (ProductionType Type, int Amount)[] savedUpgradeSpent)
    {
        var house = new House(option.houseConfig, citizenManager, resourcesManager, upgradeState, economyConfig);
        house.RestoreState(savedUpgradeCount, savedConstructPaid, savedUpgradeSpent);
        return house;
    }

    public void Demolish(RegionFacilitySlots region, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= region.Slots.Count) return;

        var occupant = region.Slots[slotIndex].Occupant;
        if (occupant == null) return;

        region.TryClear(slotIndex);

        if (occupant is ProductionFacility facility) facility.Release();
        else if (occupant is House house) house.Release();
    }
}
