using UnityEngine;

/// <summary>
/// 상태이상 디버프. 대상의 IStunAble에 위임한다 — 만료 시각·애니·이펙트는 구현체가 알아서 한다.
///
/// EnemyBase.Stun은 이미 자기 장부에 기록하므로 DebuffSO.Apply의 장부 기록과 겹치는데,
/// 값이 같아 "더 긴 쪽만 유지"에 걸려 아무 일도 일어나지 않는다. 시계는 여전히 장부 하나다.
/// 반대로 Hero는 장부가 없으므로 그쪽에선 DebuffSO의 기록이 유일한 조회 수단이 된다.
///
/// Root(속박)는 IStunAble에 해당 계약이 없어 이 SO로 걸 수 없다 —
/// "이동만 막고 공격은 허용"을 구현체가 표현할 방법이 생겨야 한다.
/// </summary>
[CreateAssetMenu(menuName = "Debuff/Stun Debuff", fileName = "StunDebuff")]
public class StunDebuffSO : DebuffSO
{
    public override DebuffType AllowedTypes => DebuffType.Stun;

    // scale은 쓰지 않는다 — 스턴의 세기는 지속시간 하나이므로 durationOverride로 조절한다.
    protected override bool OnApply(in DebuffContext ctx, float duration, float scale)
    {
        if (ctx.stunnable == null) return false;   // 영웅은 아직 IStunAble을 구현하지 않아 여기서 걸러진다

        ctx.stunnable.Stun(duration);
        return true;
    }
}
