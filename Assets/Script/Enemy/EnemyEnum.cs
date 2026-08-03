using UnityEngine;

public enum EnemyType
{
    Melee,
    Ranged,
}
public enum EnemyClass
{
    Normal,
    Elite,
    Boss,
}
// 적 특성 — 서로 조합 가능하므로 비트 플래그. CSV엔 '|' 또는 ';'로 여러 개 표기(예: "Fly|Cloaking").
[System.Flags]
public enum EnemyAttribute
{
    None     = 0,
    Cloaking = 1 << 0, // 은신: 저지당했을 때만 피격 가능
    Fly      = 1 << 1, // 공중: 원거리 영웅만 타격 가능
    UnJudged = 1 << 2, // 무시: 저지 불가(막는 영웅을 통과)
    Berserk  = 1 << 3, // 폭주: 체력50%이하 일경우 이속+공격력 증가
    Regeneration = 1 << 4, // 재생: 체력이 빠르게 참
    HitsShield = 1 << 5, //타수 보호막: 데미지1고정으로 일정 타수로만 피해를받음
    Burrow = 1 << 6, //잠행 : 은신이랑 비슷 사막전용
}
// 체력바 아래 아이콘으로 소환할 디버프 종류. 스탯이 깎였는지로 판정하므로 스탯 하나에 하나씩 대응한다(Stun만 예외).
// 스탯 감소로 나타나는 종류는 EnemyDebuffBar.TryStatOf에 대응 StatType을 같이 등록해야 표시된다(안 하면 안 뜬다).
public enum EnemyDebuffKind
{
    Slow,       // 둔화: 이속 감소 — 현재 게임에 존재하는 유일한 적 디버프(둔화 장판, 검사 관통공격)
    AtkDown,    // 공격력 감소
    DefDown,    // 방어력 감소
    AsDown,     // 공속 감소
    MaxHpDown,  // 최대 체력 감소
    Stun,       // 스턴 — 스탯이 아니라 IsStunned로 판정. IStunAble.Stun 호출자가 아직 없어 지금은 뜨지 않는다
    Poison, //독 - 지속판정을받음
    Ignite, //점화 - 화염 데미지

}
