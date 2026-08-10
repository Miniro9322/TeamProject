using System;
using System.Collections.Generic;
using UnityEngine;

// 티어별 업그레이드 기준 비용/증가율/스탯 증가치를 설정하는 애셋. 모든 영웅이 하나를 공유하며,
// 레벨은 개체가 아니라 HeroTierUpgradeState가 티어 단위로 관리한다.
[CreateAssetMenu(fileName = "HeroUpgradeConfig", menuName = "HeroData/HeroUpgradeConfig")]
public class HeroUpgradeConfig : ScriptableObject
{
    public int maxLevel = 99;
    public List<HeroTierUpgradeEntry> tierEntries;

    [SerializeField] private List<BaseUpgradeData> upgradeCostUpgrades;
    public IReadOnlyList<BaseUpgradeData> UpgradeCostUpgrades => upgradeCostUpgrades;

    public HeroTierUpgradeEntry GetEntry(int tier) => tierEntries.Find(e => e.tier == tier);
}

[Serializable]
public class HeroTierUpgradeEntry
{
    public int tier;
    public List<ResourceCost> baseCost;
    [Tooltip("레벨당 비용 증가량(고정값). 예: 10 = 레벨당 +10")]
    public int costGrowthPerLevel = 10;
    public List<HeroStatGain> statGains;

    public (ProductionType Type, int Amount)[] GetCostForLevel(int currentLevel)
    {
        var result = new (ProductionType, int)[baseCost.Count];
        for (int i = 0; i < baseCost.Count; i++)
            result[i] = (baseCost[i].Type, -(baseCost[i].Amount + costGrowthPerLevel * currentLevel));
        return result;
    }
}

[Serializable]
public class HeroStatGain
{
    public StatType statType;
    public ModifierType modifierType = ModifierType.Flat;
    public float amountPerLevel;
}
