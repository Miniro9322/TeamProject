using UnityEngine;

/// <summary>
/// 지속 피해 디버프. Poison·Ignite·Bleed는 수치만 다르므로 이 하나의 에셋 인스턴스로 만든다.
/// 틱은 DotRegistry가 굴린다 — 이 SO는 값만 넘긴다.
///
/// 틱 피해는 고정값이 아니라 <b>대상 최대 체력의 비율</b>이다. 고정값이던 시절엔 같은 독이
/// 체력 100짜리 잡몹에겐 치명적이고 5000짜리 보스에겐 없는 것과 같았고, 적 체력이 날마다
/// UpHealthScale로 불어나는 동안 지속 피해만 제자리였다. 비율로 두면 표를 안 고쳐도 같이 따라간다.
/// </summary>
[CreateAssetMenu(menuName = "Debuff/Dot Debuff", fileName = "DotDebuff")]
public class DotDebuffSO : DebuffSO
{
    [Tooltip("한 번 틱에 들어가는 피해 — 대상 최대 체력의 몇 %인가(2 = 2%, 0.5 = 0.5%). " +
             "방어력은 적용되지 않는다. 실제 피해값은 틱마다 대상의 현재 최대 체력으로 다시 계산된다.")]
    [Min(0.01f)] public float percentPerTick = 1f;

    [Tooltip("틱 간격(초). 0.05초 미만은 DotRegistry가 0.05로 보정한다.")]
    [Min(0.05f)] public float interval = 1f;

    public override DebuffType AllowedTypes =>
        DebuffType.Poison | DebuffType.Ignite | DebuffType.Bleed;

    // 이펙트 표시는 DotRegistry가 전담한다 — 매 프레임 살아있는 목록을 DebuffEffectView에 밀어 주고,
    // 만료·사망·낮 전환으로 일찍 끊기는 경우까지 그쪽이 챙긴다. DebuffSO.Apply가 또 기록하면 시계가 둘이 된다.
    protected override bool DrivesOwnEffectView => true;

    protected override bool OnApply(in DebuffContext ctx, float duration, float scale)
    {
        if (ctx.damageable == null) return false;

        // 최소 1 피해 보장은 DotRegistry가 실제 피해로 환산하는 시점(틱)에 한다 —
        // 여기선 비율만 넘기므로 반올림할 것이 없다.
        // Apply의 결과를 그대로 돌려준다. 최대 체력을 못 읽어 거절당한 경우까지 true를 주면
        // 아무 피해도 안 들어가는 디버프가 장부와 아이콘에만 남는다("보이는 것 == 걸린 것"이 깨진다).
        return DotRegistry.Apply(ctx.damageable, type, percentPerTick * scale, interval, duration, ctx.gameManager);
    }
}
