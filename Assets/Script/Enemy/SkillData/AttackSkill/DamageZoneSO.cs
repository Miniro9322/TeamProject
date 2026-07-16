using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class DamageZoneSO : AttackSkillDataSO
{
    // 배경 오라 — 죽을 때까지 지속되므로 일반 공격 루프를 막지 않는다(안 그러면 평생 공격 불가).
    public override bool BlocksBasicAttack => false;

    public override async UniTask Execute(EnemyBase owner, CancellationToken token)
    {
        if (owner == null || owner.IsDead) return;
        int cellRange = range > 0f ? Mathf.RoundToInt(range) : 1;
        Vector3 center = owner.transform.position; // 나중에 장판 이펙트/파티클 스폰 위치로 사용 예정
        float t = 0f;
        while(!owner.IsDead)
        {
            t +=Time.deltaTime;
            if(t>tickInterval)
            {
                if (owner.Board == null) return;
                Vector2Int origin = owner.Board.WorldToCell(owner.transform.position);
                List<GameObject> target = new();
                foreach (Tile tile in owner.Board.GetTiles(origin, cellRange))
                {
                    if (tile.OccupantObject == null) continue;
                    target.Add(tile.OccupantObject);
                }
                int d = Mathf.RoundToInt(damage);
                foreach(var g in target)
                {
                    // 점유 오브젝트가 항상 피격 가능한 건 아님(건물 등) — 있을 때만 데미지.
                    if (g.GetComponentInParent<IDamageAble>() is IDamageAble dmg)
                        dmg.TakeDamage(d);
                }
                t = 0;
            }
            await UniTask.Yield(token);
        }
    }
}
