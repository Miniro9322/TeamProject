using System;
using UnityEngine;

[Serializable]
public struct ZoneStatEffect
{
    [Tooltip("이 효과가 적용될 스탯 종류.")]
    public StatType statType;

    [Tooltip("Flat/Additive/Multiplier 중 적용 방식.")]
    public ModifierType modifierType;
    
    public float amount;
}
