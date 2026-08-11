using System;
using UnityEngine;

[Serializable]
public struct ZoneStatEffect
{
    [Tooltip("이 효과가 적용될 스탯 종류.")]
    public StatType statType;

    [Tooltip("퍼센트로 얼마나 바뀌는지. 예: -20 = 공격력 20% 감소, 20 = 공격력 20% 증가.")]
    public float percentAmount;
}
