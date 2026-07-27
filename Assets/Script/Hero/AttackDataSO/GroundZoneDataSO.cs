using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GroundZoneData", menuName = "HeroAttack/GroundZoneData")]
public class GroundZoneDataSO : ScriptableObject
{
    public GroundZoneMode mode = GroundZoneMode.Damage;
    public RangeShape shape = RangeShape.Diamond;
    public int radius = 1;
    public float tickInterval = 1f;
    public float damagePer = 0.5f; // mode==Damage: 틱 1회당 데미지 = casterATK * damagePer
    public float healPer = 0f;     // mode==Heal: 틱 1회당 힐량 = casterATK * healPer (범위 내 최저 체력 아군 1명)
    public float duration = 3f;    // 0 이하 = 오라 전용(소유자가 죽을 때까지 유지). 공격 트리거형 장판은 반드시 양수로 설정.
    public List<ZoneDebuffEffect> debuffs; // mode==Heal일 때는 미사용
}

public enum GroundZoneMode { Damage, Heal }

[System.Serializable]
public struct ZoneDebuffEffect
{
    public StatType statType;
    public ModifierType modifierType;
    public float value;
    public int maxStacks;
}
