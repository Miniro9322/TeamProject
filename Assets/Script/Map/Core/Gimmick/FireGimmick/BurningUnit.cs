using System.Collections.Generic;
using UnityEngine;

// 불타는 유닛 한 명의 확정된 상태를 보관한다.
public sealed class BurningUnit
{
    private readonly HashSet<Tile> activeFireTiles;

    public GameObject UnitObject { get; }
    public IDamageAble DamageTarget { get; }
    public float NextDamageTime { get; private set; }
    public float BurnExpirationTime { get; private set; }
    public bool HasActiveFireTile => activeFireTiles.Count > 0;
    public bool IsUnavailable => UnitObject == null || DamageTarget.Hp <= 0f;

    public BurningUnit(
        GameObject unitObject,
        IDamageAble damageTarget,
        Tile firstFireTile,
        float firstDamageTime)
    {
        UnitObject = unitObject;
        DamageTarget = damageTarget;
        activeFireTiles = new HashSet<Tile> { firstFireTile };
        NextDamageTime = firstDamageTime;
    }

    public void RegisterFireTile(Tile fireTile)
    {
        activeFireTiles.Add(fireTile);
    }

    public void RemoveFireTile(Tile fireTile)
    {
        activeFireTiles.Remove(fireTile);
    }

    public void ApplyNextDamageTime(float calculatedNextDamageTime)
    {
        NextDamageTime = calculatedNextDamageTime;
    }

    public void ApplyBurnExpirationTime(float calculatedBurnExpirationTime)
    {
        BurnExpirationTime = calculatedBurnExpirationTime;
    }
}
