using UnityEngine;

public enum DebuffType
{
    None = 0, // 없음
    //능력치 류 감소
    Slow = 1 << 0, // 이동속도 감소
    ATKDown = 1<< 1, // 공격력 감소
    ASDown = 1 << 2, //공격속도 감소
    ArmorBreak = 1 << 3, // 방어력 감소
    //데미지류 디버프
    Poison = 1 << 4, //독
    Ignite = 1 << 5, //점화
    Bleed = 1 << 6, //출혈
    //상태이상 디버프
    Stun = 1 << 7, //기절 (공격 스킬 불가)
    Root = 1 << 8, //속박 (공격 스킬은 가능 이동은 불가 스킬이 이동류 일경우 사용x)
    Exhaust = 1 << 9, //탈진 (공속 이속 데미지 감소)
}
