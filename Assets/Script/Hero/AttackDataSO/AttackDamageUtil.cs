using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public static class AttackDamageUtil
{
    // 적의 bodyEffectAnchor(EnemyBase)가 있으면 그 위치, 없으면(적이 아니거나 앵커 미설정) 기존처럼
    // 루트 transform.position — 사거리/AoE 판정에는 쓰지 않고 이펙트·투사체 유도 좌표 전용.
    public static Vector3 EffectPosition(GameObject go) =>
        go.GetComponentInParent<EnemyBase>() is EnemyBase enemy ? enemy.BodyEffectAnchor.position : go.transform.position;

    public static Vector3 EffectPosition(Component c) => EffectPosition(c.gameObject);

    // 대상이 살아있는 동안은 EffectPosition을 다시 읽고, 파괴되거나 풀에 반납되면 마지막 위치에
    // 고정한다 — 빔/체인 이펙트(BeamLinkEffect.Track)가 매 프레임 스스로 위치를 갱신할 때 쓴다.
    public static Func<Vector3> TrackingPosition(GameObject target)
    {
        Vector3 last = EffectPosition(target);
        return () =>
        {
            if (target != null) last = EffectPosition(target);
            return last;
        };
    }

    public static async UniTask ApplyInstantDamage(AttackDataSO data, AttackContext ctx, CancellationToken ct)
    {
        float baseDamage = ctx.sc[StatType.ATK] * data.attackPer;

        // ctx.hero.GetObjectsInRange를 부분적용한 어댑터 — ApplyHealOptions/ChainResolver는 여전히
        // Func<Vector3,int,RangeShape,List<GameObject>> 3-인자 시그니처를 기대한다.
        List<GameObject> AllyQuery(Vector3 p, int r, RangeShape s) => ctx.hero.GetObjectsInRange(p, r, s, RangeQueryAffinity.Ally);
        List<GameObject> TargetableEnemyQuery(Vector3 p, int r, RangeShape s) => ctx.hero.GetObjectsInRange(p, r, s, RangeQueryAffinity.TargetableEnemy);

        ApplySelfBuffs(ctx.hero, data.buffList, ctx.buffManager, data);

        if (data.attackType == AttackType.Area && data.areaShape == AreaShape.Line)
        {
            for (int i = 0; i < data.attackCount; i++)
            {
                foreach (IDamageAble e in ctx.hero.GetEnemiesInLine(ctx.self.position, ctx.target.position, data.lineLength, data.areaRange))
                {
                    e.TakeDamage((int)baseDamage);
                    ctx.hero.NotifyHit((e as Component)?.gameObject, (int)baseDamage, false);
                    ApplyTargetDebuffs(e as Component, data.targetDebuffs, ctx.buffManager, data);
                    ApplyHealOptions(data, ctx.self.position, ctx.hero.Heal, AllyQuery, baseDamage, ctx.sc[StatType.ATK]);
                    ctx.hero.SpawnEffect(data.hitEffect, EffectPosition(e as Component), Quaternion.identity, data.hitEffectLifetime);
                }
                if (i < data.attackCount - 1)
                    await UniTask.Delay(TimeSpan.FromSeconds(data.shotInterval), cancellationToken: ct);
            }
            return;
        }

        if (data.areaShape == AreaShape.Chain)
        {
            List<GameObject> hits = ChainResolver.Resolve(ctx.target.gameObject, baseDamage, data.chainRange, data.chainCount,
                data.chainFalloff, TargetableEnemyQuery, ctx.hero.NotifyHit);
            foreach (GameObject go in hits)
            {
                ApplyTargetDebuffs(go.transform, data.targetDebuffs, ctx.buffManager, data);
                ApplyHealOptions(data, ctx.self.position, ctx.hero.Heal, AllyQuery, baseDamage, ctx.sc[StatType.ATK]);
                ctx.hero.SpawnEffect(data.hitEffect, EffectPosition(go), Quaternion.identity, data.hitEffectLifetime);
            }
            // hits[0]은 체인 시작 타겟(캐스터→시작 타겟 구간은 빔 비주얼 등 별도 이펙트가 표현) —
            // 튕긴 대상들 사이(hits[i]→hits[i+1])만 아크로 잇는다.
            for (int i = 0; i < hits.Count - 1; i++)
                ctx.hero.SpawnChainArc(data.chainEffectPrefab, hits[i], hits[i + 1], data.chainEffectLifetime);
            return;
        }

        if (data.attackType == AttackType.Single && data.targetMode == TargetMode.SameTarget)
        {
            for (int i = 0; i < data.attackCount; i++)
            {
                if (ctx.target.GetComponent<IDamageAble>() is IDamageAble d)
                {
                    d.TakeDamage((int)baseDamage);
                    ctx.hero.NotifyHit(ctx.target.gameObject, (int)baseDamage, false);
                    ApplyTargetDebuffs(ctx.target, data.targetDebuffs, ctx.buffManager, data);
                    ApplyHealOptions(data, ctx.self.position, ctx.hero.Heal, AllyQuery, baseDamage, ctx.sc[StatType.ATK]);
                    ctx.hero.SpawnEffect(data.hitEffect, EffectPosition(ctx.target), Quaternion.identity, data.hitEffectLifetime);
                }
                if (i < data.attackCount - 1)
                    await UniTask.Delay(TimeSpan.FromSeconds(data.shotInterval), cancellationToken: ct);
            }
            return;
        }

        if (data.attackType == AttackType.Single && data.targetMode == TargetMode.DifferentEnemies)
        {
            List<GameObject> enemies = ctx.hero.GetObjectsInRange(ctx.self.position, data.range, data.rangeShape, RangeQueryAffinity.TargetableEnemy);
            List<GameObject> targets = AttackTargetSelector.SelectTargets(enemies, data.attackCount, data.targetCount);
            await FireEach(targets, t =>
            {
                if (t.GetComponentInParent<IDamageAble>() is not IDamageAble d) return;
                d.TakeDamage((int)baseDamage);
                ctx.hero.NotifyHit(t, (int)baseDamage, false);
                ApplyTargetDebuffs(t.transform, data.targetDebuffs, ctx.buffManager, data);
                ApplyHealOptions(data, ctx.self.position, ctx.hero.Heal, AllyQuery, baseDamage, ctx.sc[StatType.ATK]);
                ctx.hero.SpawnEffect(data.hitEffect, EffectPosition(t), Quaternion.identity, data.hitEffectLifetime);
            }, data.shotInterval, ct);
            return;
        }

        RangeShape aoeShape = ResolveAoeShape(data);

        if (data.attackType == AttackType.Area && data.targetMode == TargetMode.SameTarget)
        {
            Vector3 aoeCenter = data.areaCenterOnTarget && ctx.target != null ? ctx.target.position : ctx.self.position;
            for (int i = 0; i < data.attackCount; i++)
            {
                foreach (GameObject go in ctx.hero.GetObjectsInRange(aoeCenter, data.areaRange, aoeShape, RangeQueryAffinity.Enemy))
                {
                    if (go.GetComponentInParent<IDamageAble>() is not IDamageAble e) continue;
                    e.TakeDamage((int)baseDamage);
                    ctx.hero.NotifyHit(go, (int)baseDamage, false);
                    ApplyTargetDebuffs(go.transform, data.targetDebuffs, ctx.buffManager, data);
                    ApplyHealOptions(data, ctx.self.position, ctx.hero.Heal, AllyQuery, baseDamage, ctx.sc[StatType.ATK]);
                    ctx.hero.SpawnEffect(data.hitEffect, EffectPosition(go), Quaternion.identity, data.hitEffectLifetime);
                }
                if (i < data.attackCount - 1)
                    await UniTask.Delay(TimeSpan.FromSeconds(data.shotInterval), cancellationToken: ct);
            }
            return;
        }

        List<GameObject> enemyObjects = TargetableEnemyQuery(ctx.self.position, data.range, data.rangeShape);
        List<GameObject> centers = AttackTargetSelector.SelectTargets(enemyObjects, data.attackCount, data.targetCount);
        await FireEach(centers, go =>
        {
            foreach (GameObject hit in ctx.hero.GetObjectsInRange(go.transform.position, data.areaRange, aoeShape, RangeQueryAffinity.Enemy))
            {
                if (hit.GetComponentInParent<IDamageAble>() is not IDamageAble e) continue;
                e.TakeDamage((int)baseDamage);
                ctx.hero.NotifyHit(hit, (int)baseDamage, false);
                ApplyTargetDebuffs(hit.transform, data.targetDebuffs, ctx.buffManager, data);
                ApplyHealOptions(data, ctx.self.position, ctx.hero.Heal, AllyQuery, baseDamage, ctx.sc[StatType.ATK]);
                ctx.hero.SpawnEffect(data.hitEffect, EffectPosition(hit), Quaternion.identity, data.hitEffectLifetime);
            }
        }, data.shotInterval, ct);
    }

    // Area 공격의 AOE 판정 모양 — Square만 실제 사각형, 그 외(Diamond/Line/Chain 오분류 방지용 기본값)는
    // 전부 Diamond로 취급한다. ApplyInstantDamage의 자기중심/다중센터 AOE 분기와 ContinuousBeamStrategy의
    // SelfArea 종료 판정(주변에 적이 남아있는지 체크)이 서로 다른 모양을 쓰면 안 되므로 한 곳에 모은다.
    public static RangeShape ResolveAoeShape(AttackDataSO data) =>
        data.areaShape == AreaShape.Square ? RangeShape.Square : RangeShape.Diamond;

    // Healer 전용 — TakeDamage 대신 Heal을 적용한다. 힐은 "적중"이 아니므로 onHit 훅을 부르지 않는다.
    public static UniTask ApplyInstantHeal(AttackDataSO data, AttackContext ctx, CancellationToken ct)
    {
        float healAmount = ctx.sc[StatType.ATK] * data.attackPer;
        ApplySelfBuffs(ctx.hero, data.buffList, ctx.buffManager, data);

        if (data.attackType == AttackType.Single)
        {
            if (ctx.target != null && ctx.target.GetComponent<Hero>() is Hero singleAlly)
            {
                singleAlly.Heal(healAmount);
                ctx.hero.SpawnEffect(data.hitEffect, ctx.target.position, Quaternion.identity, data.hitEffectLifetime);
            }
            return UniTask.CompletedTask;
        }

        RangeShape aoeShape = ResolveAoeShape(data);
        foreach (GameObject go in ctx.hero.GetObjectsInRange(ctx.self.position, data.areaRange, aoeShape, RangeQueryAffinity.Ally))
            if (go.GetComponent<Hero>() is Hero areaAlly)
            {
                areaAlly.Heal(healAmount);
                ctx.hero.SpawnEffect(data.hitEffect, areaAlly.transform.position, Quaternion.identity, data.hitEffectLifetime);
            }

        return UniTask.CompletedTask;
    }

    public static void ApplyHealOptions(AttackDataSO data, Vector3 selfPos,
        Action<float> healSelf,
        Func<Vector3, int, RangeShape, List<GameObject>> getAllyObjectsInRange,
        float damageDealt,
        float casterAtk)
    {
        if (data.lifestealPercent > 0f && damageDealt > 0f)
            healSelf?.Invoke(damageDealt * data.lifestealPercent);

        if (data.allyHealAmount > 0f && getAllyObjectsInRange != null)
        {
            Hero target = FindLowestHpAlly(getAllyObjectsInRange(selfPos, data.allyHealRange, data.allyHealRangeShape));
            target?.Heal(casterAtk * data.allyHealAmount);
        }
    }

    // 다친(풀피가 아닌) 후보 중 Hp가 가장 낮은 대상. 풀피 아군을 먼저 제외해야 "최대 체력이 작은 풀피
    // 영웅"이 "다쳤지만 최대 체력이 큰 영웅"보다 절대 Hp가 낮게 나와 잘못 선택되는 문제를 피한다.
    // Healer.AcquireTargetFromTiles / GroundZoneEffect 힐 틱에서도 재사용.
    public static Hero FindLowestHpAlly(List<GameObject> candidates)
    {
        Hero lowest = null;
        float lowestHp = float.MaxValue;
        foreach (GameObject go in candidates)
        {
            if (go.GetComponentInParent<Hero>() is Hero d
                && d.Hp < d.SC[StatType.HP]
                && d.Hp < lowestHp)
            {
                lowestHp = d.Hp;
                lowest = d;
            }
        }
        return lowest;
    }

    public static void ApplySelfBuffs(IUnit selfUnit, List<BuffEffect> buffList, BuffManager buffManager, object source)
    {
        if (buffList == null || selfUnit == null) return;
        foreach (BuffEffect effect in buffList)
        {
            buffManager.ApplyStackingModifier(selfUnit, effect.statType, effect.modifierType,
                effect.value, effect.duration, effect.maxStacks, source);
        }
    }

    // 적 디버프 시스템(DebuffSO/DebuffContext/DebuffTracker)을 그대로 재사용한다 — EnemyBase.ApplyDebuff와
    // 하는 일이 같고, DebuffApply.To가 유일한 공용 입구다. 이걸 거쳐야 DebuffTracker 장부·면역·UI 아이콘이
    // 반영된다(예전처럼 BuffManager를 직접 호출하면 스탯만 바뀌고 장부에는 안 남는다).
    public static void ApplyTargetDebuffs(Component target, List<TargetDebuffRef> targetDebuffs, BuffManager buffManager, object source)
    {
        if (target == null || targetDebuffs == null) return;
        foreach (TargetDebuffRef entry in targetDebuffs)
        {
            if (entry.debuff == null) continue;
            float scale = entry.scale > 0f ? entry.scale : 1f;
            DebuffApply.To(target, entry.debuff, buffManager, null, entry.durationOverride, scale, source);
        }
    }

    private static async UniTask FireEach<T>(List<T> items, Action<T> apply, float interval, CancellationToken ct)
    {
        for (int i = 0; i < items.Count; i++)
        {
            apply(items[i]);
            if (i < items.Count - 1)
                await UniTask.Delay(TimeSpan.FromSeconds(interval), cancellationToken: ct);
        }
    }
}
