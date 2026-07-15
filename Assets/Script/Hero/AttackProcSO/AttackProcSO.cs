using UnityEngine;

public enum ProcEffectType
{
    ExtraAttack,        // procAttack을 딜레이 없이 즉시 재실행
    UpgradeNextAttack,  // 다음 정규 공격을 procAttack으로 교체 (공격속도 쿨타임은 정상 대기)
    BonusDamage,        // 애니메이션 없이 procAttack.attackPer 기준 데미지만 즉시 적용
}

public abstract class AttackProcSO : ScriptableObject
{
    public AttackDataSO triggerOnAttack;  // null = 아무 공격에나 반응
    public AttackDataSO procAttack;       // null = 트리거 공격 반복
    public bool allowRecursiveProc = false;
    public ProcEffectType effectType = ProcEffectType.ExtraAttack;

    public abstract bool ShouldProc(AttackDataSO source, AttackContext ctx);

    public AttackDataSO GetProcAttack(AttackDataSO source) => procAttack != null ? procAttack : source;
}
