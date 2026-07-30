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

        GameObject landEffectGo = spawnPersistentEffect(data.landEffect, center, Quaternion.Euler(data.landEffectRotation));
        ApplyLandEffectScale(landEffectGo, data.radius, data.landEffectRadius);
        if (!persistent) FitToDuration(landEffectGo, data.duration);
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

    // landEffect의 각 자식 파티클 시스템을 duration에 맞춘다. 이미 반복(loop)으로 authored된 자식은
    // 그대로 자연스러운 속도로 계속 반복되게 두고(예: Trail), 1회성(loop=false)으로 authored된 자식만
    // "자기 자신의 원래 duration → 목표 duration" 배율로 simulationSpeed를 조정해 정확히 한 번 재생하고
    // 끝나도록 한다. 여러 자식마다 원래 길이가 제각각이므로(RainLightning처럼) 배율은 자식별로 따로 계산한다.
    private static void FitToDuration(GameObject go, float targetDuration)
    {
        if (go == null || targetDuration <= 0f) return;
        foreach (ParticleSystem ps in go.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = ps.main;
            if (main.loop) continue; // 원래도 반복되는 자식은 그대로 둔다.
            if (main.duration <= 0f) continue;
            main.simulationSpeed = main.duration / targetDuration;
        }
    }

    // landEffectRadius(프리팹이 기본 크기로 나타내는 반경, 타일 수)를 기준으로 실제 radius에 맞게
    // 균일 스케일한다. 예: landEffectRadius=1인데 radius=3이면 3배 크기로 표시.
    private static void ApplyLandEffectScale(GameObject go, int radius, float landEffectRadius)
    {
        if (go == null || landEffectRadius <= 0f) return;
        go.transform.localScale = Vector3.one * (radius / landEffectRadius);
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
            {
                d.TakeDamage(dmg);
                spawnEffect(data.hitEffect, go.transform.position, Quaternion.identity, data.hitEffectLifetime);
            }

            // NOTE: EnemyBase가 아직 IUnit을 구현하지 않아(기존 버그, 별도 작업 예정) 아래는 현재 실제 적에겐 no-op.
            if (data.debuffs != null && go.GetComponentInParent<IUnit>() is IUnit unit)
                foreach (ZoneDebuffEffect debuff in data.debuffs)
                    buffManager.ApplyStackingModifier(unit, debuff.statType, debuff.modifierType,
                        debuff.value, debuffDuration, debuff.maxStacks, source);
        }
    }
}
