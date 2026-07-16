using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

public enum EffectType
{
    None,
    Dash,
    Teleport,
    Heal,
    Shield,
    Summon,
    Split,
    Stealth,
}

public abstract class SkillDataSO : ScriptableObject
{
    public string skillName; 
    public float cooldown;
    public float duration;
    public float range;
    public float tickInterval;

    // 이 스킬이 "실행 중"인 동안 일반 공격을 막을지 여부.
    // 애니메이션을 재생하는 스킬(Dash/Summon/Shield 등)은 true로 두어 애니메이터 충돌을 막고,
    // 애니메이션 없이 계속 도는 배경 오라(DamageZone 등)는 false로 오버라이드해 일반 공격을 막지 않게 한다.
    public virtual bool BlocksBasicAttack => true;

    public abstract UniTask Execute(EnemyBase owner, CancellationToken token);
}
