using UnityEngine;

/// <summary>
/// 지속 피해 디버프. Poison·Ignite·Bleed는 수치만 다르므로 이 하나의 에셋 인스턴스로 만든다.
/// 틱은 DotRegistry가 굴린다 — 이 SO는 값만 넘긴다.
/// </summary>
[CreateAssetMenu(menuName = "Debuff/Dot Debuff", fileName = "DotDebuff")]
public class DotDebuffSO : DebuffSO
{
    [Tooltip("한 번 틱에 들어가는 피해. 방어력은 적용되지 않는다.")]
    [Min(1)] public int damagePerTick = 5;

    [Tooltip("틱 간격(초). 0.05초 미만은 DotRegistry가 0.05로 보정한다.")]
    [Min(0.05f)] public float interval = 1f;

    public override DebuffType AllowedTypes =>
        DebuffType.Poison | DebuffType.Ignite | DebuffType.Bleed;

    protected override bool OnApply(in DebuffContext ctx, float duration, float scale)
    {
        if (ctx.damageable == null) return false;

        // 배율을 곱한 뒤에도 최소 1은 들어가게 한다 — 0이 되면 DotRegistry가 받지 않아 디버프가 조용히 사라진다.
        int damage = Mathf.Max(1, Mathf.RoundToInt(damagePerTick * scale));

        DotRegistry.Apply(ctx.damageable, type, damage, interval, duration, ctx.gameManager);
        return true;
    }
}
