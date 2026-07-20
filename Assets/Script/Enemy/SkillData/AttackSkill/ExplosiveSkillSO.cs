using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class ExplosiveSkillSO : AttackSkillDataSO
{
    public override bool TriggerOnDeath => true;
    public override async UniTask Execute(EnemyBase owner, CancellationToken token)
    {
        if (owner == null) return;
        if (owner.Board == null) return;
        Vector2Int origin = owner.Board.WorldToCell(owner.transform.position);
        foreach (Tile tile in owner.Board.GetTiles(origin,Mathf.FloorToInt(range)))
        {
            if (tile.OccupantObject == null) continue;
            if (tile.OccupantObject.GetComponentInParent<IDamageAble>() is IDamageAble target)
            {
                target.TakeDamage(Mathf.FloorToInt(damage));
            }
        }
        await UniTask.CompletedTask;
    }
}