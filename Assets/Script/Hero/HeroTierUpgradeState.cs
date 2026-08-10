using System;
using System.Collections.Generic;

// 티어별 업그레이드 진행도(레벨)를 들고 있는 런타임 싱글턴. 레벨은 영웅 개체가 아니라 티어에 속하므로
// 영웅 GameObject가 파괴/재생성돼도(제거·재배치, 합성 등) 리셋되지 않는다.
public class HeroTierUpgradeState
{
    private readonly HeroUpgradeConfig config;
    private readonly ResourcesManager resourcesManager;
    private readonly UpgradeState upgradeState;
    private readonly Dictionary<int, int> levels = new();

    public event Action<int> LevelChanged; // 인자: 티어

    private float CostDiscount => upgradeState.GetTotalEffect(config.UpgradeCostUpgrades);

    public HeroTierUpgradeState(HeroUpgradeConfig config, ResourcesManager resourcesManager, UpgradeState upgradeState)
    {
        this.config = config;
        this.resourcesManager = resourcesManager;
        this.upgradeState = upgradeState;
    }

    public int GetLevel(int tier) => levels.TryGetValue(tier, out int lvl) ? lvl : 0;

    public bool CanLevelUp(int tier)
    {
        var entry = config.GetEntry(tier);
        return entry != null && GetLevel(tier) < config.maxLevel;
    }

    public (ProductionType Type, int Amount)[] GetNextLevelCost(int tier)
        => GetCostForLevel(tier, GetLevel(tier));

    public (ProductionType Type, int Amount)[] GetCostForLevel(int tier, int level)
        => config.GetEntry(tier)?.GetCostForLevel(level).ApplyDiscount(CostDiscount) ?? Array.Empty<(ProductionType, int)>();

    public IReadOnlyList<HeroStatGain> GetStatGains(int tier)
        => (IReadOnlyList<HeroStatGain>)config.GetEntry(tier)?.statGains ?? Array.Empty<HeroStatGain>();

    public bool TryLevelUp(int tier)
    {
        if (!CanLevelUp(tier)) return false;
        var cost = GetNextLevelCost(tier);
        if (!resourcesManager.CheckResources(cost)) return false;

        resourcesManager.ProductChanged(cost);
        levels[tier] = GetLevel(tier) + 1;
        LevelChanged?.Invoke(tier);
        return true;
    }
}
