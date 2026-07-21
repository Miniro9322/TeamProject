using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GroundZoneData", menuName = "HeroAttack/GroundZoneData")]
public class GroundZoneDataSO : ScriptableObject
{
    public RangeShape shape = RangeShape.Diamond;
    public int radius = 1;
    public float tickInterval = 1f;
    public float damagePer = 0.5f; // 틱 1회당 데미지 = heroATK * damagePer
    public float duration = 3f;    // 0 이하 = 오라 전용(소유자가 죽을 때까지 유지). 공격 트리거형 장판은 반드시 양수로 설정.
    public List<ZoneDebuffEffect> debuffs;
}

[System.Serializable]
public struct ZoneDebuffEffect
{
    public StatType statType;
    public ModifierType modifierType;
    public float value;
    public int maxStacks;
}
