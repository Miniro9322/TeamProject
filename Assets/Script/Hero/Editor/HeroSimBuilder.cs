using System.Collections.Generic;
using System.Text;

// 원본 데이터와 입력 조건을 받아 표 한 줄(HeroSimResult)을 조립하는 담당.
// 스탯·피해·비용 계산은 HeroSimCalc에 맡기고 여기서는 값을 모아 담기만 한다.
public static class HeroSimBuilder
{
    private const string TraitSuffix = "Trait";
    private const string NoTraitLabel = "없음";
    private const string UnmodeledMark = "⚠";
    private const float PercentScale = 100f;

    private static readonly List<HeroStatGain> EmptyGains = new();

    // 영웅 한 명의 표 한 줄을 만든다.
    public static HeroSimResult BuildResult(HeroSimEntry entry, HeroUpgradeConfig tierConfig,
        HeroClassUpgradeConfig classConfig, HeroSimInput input, float titleBonus)
    {
        HeroTierUpgradeEntry tierEntry = tierConfig.GetEntry(entry.HeroData.Tier);
        HeroClassUpgradeEntry classEntry = classConfig.GetEntry(entry.HeroData.HeroType);

        Dictionary<StatType, float> stats = HeroSimCalc.CalculateStats(entry.StatData,
            TierGainsOf(tierEntry), input.TierUpgradeCount,
            ClassGainsOf(classEntry), input.ClassUpgradeCount, titleBonus);

        List<HeroSimHeavyChance> heavies = HeroSimCalc.ResolveHeavyRatios(entry);
        float attackPower = stats[StatType.ATK];
        float average = HeroSimCalc.CalculateAverageHit(entry.BasePattern, heavies, attackPower);

        return new HeroSimResult
        {
            HeroName = entry.HeroData.HeroName,
            Tier = entry.HeroData.Tier,
            MaxHp = stats[StatType.HP],
            AttackPower = attackPower,
            Defence = stats[StatType.DEF],
            AttackSpeed = stats[StatType.AS],
            BlockCount = stats[StatType.BLK],
            NormalHitDamage = HeroSimCalc.CalculateNormalHit(entry.BasePattern, attackPower),
            AverageHitDamage = average,
            DamagePerSecond = average * stats[StatType.AS],
            CumulativeCost = SumUpgradeCost(tierEntry, classEntry, input),
            TraitNote = BuildTraitNote(entry, heavies),
        };
    }

    // 티어 강화와 직업 강화에 지금까지 들어간 자원 총합.
    private static int SumUpgradeCost(HeroTierUpgradeEntry tierEntry, HeroClassUpgradeEntry classEntry, HeroSimInput input)
    {
        int total = 0;
        if (tierEntry != null)
            total += HeroSimCalc.CalculateCumulativeCost(tierEntry.GetCostForLevel, input.TierUpgradeCount);
        if (classEntry != null)
            total += HeroSimCalc.CalculateCumulativeCost(classEntry.GetCostForLevel, input.ClassUpgradeCount);
        return total;
    }

    // 티어 강화 증가치 목록. 설정이 없는 티어면 빈 목록을 준다.
    private static IReadOnlyList<HeroStatGain> TierGainsOf(HeroTierUpgradeEntry tierEntry)
    {
        if (tierEntry == null) return EmptyGains;
        return tierEntry.statGains;
    }

    // 직업 강화 증가치 목록. 설정이 없는 직업이면 빈 목록을 준다.
    private static IReadOnlyList<HeroStatGain> ClassGainsOf(HeroClassUpgradeEntry classEntry)
    {
        if (classEntry == null) return EmptyGains;
        return classEntry.statGains;
    }

    // 트레잇 요약 문구. 계산에 넣은 강공 비율과 넣지 못한 트레잇을 함께 적는다.
    private static string BuildTraitNote(HeroSimEntry entry, List<HeroSimHeavyChance> heavies)
    {
        float heavyRatio = 0f;
        for (int index = 0; index < heavies.Count; index++)
        {
            heavyRatio += heavies[index].SwingRatio;
        }

        var note = new StringBuilder();
        if (heavyRatio > 0f) note.Append($"강공 {heavyRatio * PercentScale:N0}%");

        for (int index = 0; index < entry.UnmodeledTraitNames.Count; index++)
        {
            if (note.Length > 0) note.Append(' ');
            note.Append(UnmodeledMark);
            note.Append(entry.UnmodeledTraitNames[index].Replace(TraitSuffix, string.Empty));
        }

        if (note.Length == 0) return NoTraitLabel;
        return note.ToString();
    }
}
