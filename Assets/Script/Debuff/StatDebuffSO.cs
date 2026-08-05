using UnityEngine;

/// <summary>
/// 스탯 감소 디버프. 시간 관리를 전부 BuffManager에 맡긴다 — 자체 타이머가 없다.
///
/// Slow·ASDown·ArmorBreak·Exhaust는 클래스를 따로 만들 이유가 없어 이 하나의 에셋 인스턴스로 만든다
/// (Slow = SPD 하나, Exhaust = AS·SPD·ATK 셋을 한 에셋에 넣으면 끝).
/// </summary>
[CreateAssetMenu(menuName = "Debuff/Stat Debuff", fileName = "StatDebuff")]
public class StatDebuffSO : DebuffSO
{
    [Tooltip("깎을 스탯 목록. 이속만 깎으면 1개, 탈진처럼 여러 개면 여기 늘린다.")]
    public DebuffStatEffect[] effects;

    public override DebuffType AllowedTypes =>
        DebuffType.Slow | DebuffType.ATKDown | DebuffType.ASDown | DebuffType.ArmorBreak | DebuffType.Exhaust;

    protected override bool OnApply(in DebuffContext ctx, float duration, float scale)
    {
        if (ctx.unit == null || ctx.buffManager == null || effects == null) return false;

        for (int i = 0; i < effects.Length; i++)
        {
            // scale은 감소분에 곱한다 — Additive -0.3에 scale 1.67이면 -0.5(50% 감소)가 된다.
            ctx.buffManager.ApplyStackingModifier(ctx.unit, effects[i].statType, effects[i].modifierType,
                effects[i].value * scale, duration, effects[i].maxStacks, ctx.source ?? this);
        }
        return effects.Length > 0;
    }
}

[System.Serializable]
public struct DebuffStatEffect
{
    public StatType statType;

    [Tooltip("Flat = 절대값 가감(이속 5 → 3). Additive/Multiplier = 비율. " +
             "둘 다 value가 -0.3이면 30% 감소이고, 차이는 중첩 시에만 난다 — " +
             "Additive는 합산 후 한 번 곱하고(-0.3 두 개 = x0.4), Multiplier는 각각 곱한다(x0.49).")]
    public ModifierType modifierType;

    // Stat.cs의 계산식이 value *= (1f + mod.Value)라 비율도 "감소분"을 음수로 넣는다.
    // 0.7을 넣으면 x0.7이 아니라 x1.7이 되어 디버프가 버프로 뒤집힌다.
    [Tooltip("감소분이므로 항상 음수. Flat이면 -2(절대값), 비율이면 -0.3(=30% 감소). " +
             "배율 0.7을 넣으면 x1.7이 되어 거꾸로 강화된다.")]
    public float value;

    [Tooltip("1 = 스택 없이 지속시간만 갱신. Unity 기본값 0은 BuffManager가 1로 보정한다. " +
             "비율 감소는 value x maxStacks가 -1에 닿으면 스탯이 0이 되니 주의.")]
    public int maxStacks;
}
