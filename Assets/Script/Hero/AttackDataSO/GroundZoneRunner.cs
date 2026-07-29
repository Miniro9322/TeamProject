using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 영웅 장판(Ground Zone) 틱 루프. DamageZoneSO.Execute와 동일한 패턴이지만
// Hero/AttackContext에 의존하지 않고 델리게이트만 받아 오라형·공격트리거형 양쪽에서 재사용한다.
public static class GroundZoneRunner
{
    public static async UniTask Run(
        Vector3 center,
        GroundZoneDataSO data,
        Func<Vector3, int, RangeShape, List<GameObject>> getEnemyObjectsInRange,
        Func<Vector3, int, RangeShape, List<GameObject>> getAllyObjectsInRange,
        StatContainer attackerStats,
        BuffManager buffManager,
        Func<GameObject, Vector3, Quaternion, float, GameObject> spawnEffect,
        Func<GameObject, Vector3, Quaternion, GameObject> spawnPersistentEffect,
        Action<GameObject, GameObject> despawnEffect,
        Func<bool> keepAliveWhilePersistent, // duration<=0(오라)일 때만 참조
        CancellationToken token)
    {
        if (data == null) return;

        object source = new object(); // 이 Run 호출(=이 장판 인스턴스) 전용 키 — BuffManager가 (target,stat,source)로 구분하므로 같은 SO를 쓰는 다른 장판과 섞이지 않게
        bool persistent = data.duration <= 0f;
        float elapsed = 0f, tickTimer = 0f;

        GameObject landEffectGo = spawnPersistentEffect(data.landEffect, center, Quaternion.identity);
        try
        {
            while (!token.IsCancellationRequested && (persistent ? keepAliveWhilePersistent() : elapsed < data.duration))
            {
                float dt = Time.deltaTime;
                tickTimer += dt;
                if (!persistent) elapsed += dt;

                if (tickTimer >= data.tickInterval)
                {
                    tickTimer = 0f;
                    Tick(center, data, getEnemyObjectsInRange, getAllyObjectsInRange, attackerStats, buffManager, spawnEffect, source);
                }
                await UniTask.Yield(token);
            }
        }
        finally
        {
            despawnEffect(data.landEffect, landEffectGo);
        }
    }

    private static void Tick(Vector3 center, GroundZoneDataSO data,
        Func<Vector3, int, RangeShape, List<GameObject>> getEnemyObjectsInRange,
        Func<Vector3, int, RangeShape, List<GameObject>> getAllyObjectsInRange,
        StatContainer attackerStats, BuffManager buffManager,
        Func<GameObject, Vector3, Quaternion, float, GameObject> spawnEffect, object source)
    {
        if (data.mode == GroundZoneMode.Heal)
        {
            float heal = attackerStats[StatType.ATK] * data.healPer;
            if (heal <= 0f || getAllyObjectsInRange == null) return;

            Hero target = AttackDamageUtil.FindLowestHpAlly(getAllyObjectsInRange(center, data.radius, data.shape));
            target?.Heal(heal);
            if (target != null)
                spawnEffect(data.hitEffect, center, Quaternion.identity, data.hitEffectLifetime);
            return;
        }

        int dmg = Mathf.RoundToInt(attackerStats[StatType.ATK] * data.damagePer);
        float debuffDuration = data.tickInterval + 0.15f; // 다음 틱까지 갱신 못 받으면(=영역 이탈) 곧 만료 — 이탈 시 디버프 제거를 흉내

        List<GameObject> targets = getEnemyObjectsInRange(center, data.radius, data.shape);
        foreach (GameObject go in targets)
        {
            if (dmg > 0 && go.GetComponentInParent<IDamageAble>() is IDamageAble d)
                d.TakeDamage(dmg);

            // NOTE: EnemyBase가 아직 IUnit을 구현하지 않아(기존 버그, 별도 작업 예정) 아래는 현재 실제 적에겐 no-op.
            if (data.debuffs != null && go.GetComponentInParent<IUnit>() is IUnit unit)
                foreach (ZoneDebuffEffect debuff in data.debuffs)
                    buffManager.ApplyStackingModifier(unit, debuff.statType, debuff.modifierType,
                        debuff.value, debuffDuration, debuff.maxStacks, source);
        }
        if (targets.Count > 0)
            spawnEffect(data.hitEffect, center, Quaternion.identity, data.hitEffectLifetime);
    }
}
