using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public static class AttackDamageUtil
{
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
            foreach (IDamageAble e in ctx.hero.GetEnemiesInLine(ctx.self.position, ctx.target.position, data.lineLength, data.areaRange))
            {
                e.TakeDamage((int)baseDamage);
                ctx.hero.NotifyHit((e as Component)?.gameObject, (int)baseDamage, false);
                ApplyTargetDebuffs(e as IUnit, data.buffList, ctx.buffManager, data);
                ApplyHealOptions(data, ctx.self.position, ctx.hero.Heal, AllyQuery, baseDamage, ctx.sc[StatType.ATK]);
            }
            ctx.hero.SpawnEffect(data.hitEffect, ctx.target.position, Quaternion.identity, data.hitEffectLifetime);
            return;
        }

        if (data.attackType == AttackType.Area && data.areaShape == AreaShape.Chain)
        {
            List<GameObject> hits = ChainResolver.Resolve(ctx.target.gameObject, baseDamage, data.chainRange, data.chainCount,
                data.chainFalloff, TargetableEnemyQuery, ctx.hero.NotifyHit);
            foreach (GameObject go in hits)
            {
                ApplyTargetDebuffs(go.GetComponentInParent<IUnit>(), data.buffList, ctx.buffManager, data);
                ApplyHealOptions(data, ctx.self.position, ctx.hero.Heal, AllyQuery, baseDamage, ctx.sc[StatType.ATK]);
            }
            // hits[0]은 체인 시작 타겟(캐스터→시작 타겟 구간은 빔 비주얼 등 별도 이펙트가 표현) —
            // 튕긴 대상들 사이(hits[i]→hits[i+1])만 아크로 잇는다.
            for (int i = 0; i < hits.Count - 1; i++)
                ctx.hero.SpawnChainArc(data.chainEffectPrefab, hits[i].transform.position, hits[i + 1].transform.position, data.chainEffectLifetime);
            ctx.hero.SpawnEffect(data.hitEffect, ctx.target.position, Quaternion.identity, data.hitEffectLifetime);
            return;
        }

        if (data.attackType == AttackType.Single && data.targetMode == TargetMode.SameTarget)
        {
            if (ctx.target.GetComponent<IDamageAble>() is IDamageAble d)
            {
                d.TakeDamage((int)baseDamage);
                ctx.hero.NotifyHit(ctx.target.gameObject, (int)baseDamage, false);
                ApplyTargetDebuffs(d as IUnit, data.buffList, ctx.buffManager, data);
                ApplyHealOptions(data, ctx.self.position, ctx.hero.Heal, AllyQuery, baseDamage, ctx.sc[StatType.ATK]);
                ctx.hero.SpawnEffect(data.hitEffect, ctx.target.position, Quaternion.identity, data.hitEffectLifetime);
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
                ApplyTargetDebuffs(d as IUnit, data.buffList, ctx.buffManager, data);
                ApplyHealOptions(data, ctx.self.position, ctx.hero.Heal, AllyQuery, baseDamage, ctx.sc[StatType.ATK]);
                ctx.hero.SpawnEffect(data.hitEffect, t.transform.position, Quaternion.identity, data.hitEffectLifetime);
            }, data.shotInterval, ct);
            return;
        }

        RangeShape aoeShape = data.areaShape == AreaShape.Square ? RangeShape.Square : RangeShape.Diamond;

        if (data.attackType == AttackType.Area && data.targetMode == TargetMode.SameTarget)
        {
            for (int i = 0; i < data.attackCount; i++)
            {
                foreach (GameObject go in ctx.hero.GetObjectsInRange(ctx.self.position, data.areaRange, aoeShape, RangeQueryAffinity.Enemy))
                {
                    if (go.GetComponentInParent<IDamageAble>() is not IDamageAble e) continue;
                    e.TakeDamage((int)baseDamage);
                    ctx.hero.NotifyHit(go, (int)baseDamage, false);
                    ApplyTargetDebuffs(e as IUnit, data.buffList, ctx.buffManager, data);
                    ApplyHealOptions(data, ctx.self.position, ctx.hero.Heal, AllyQuery, baseDamage, ctx.sc[StatType.ATK]);
                }
                ctx.hero.SpawnEffect(data.hitEffect, ctx.self.position, Quaternion.identity, data.hitEffectLifetime);
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
                ApplyTargetDebuffs(e as IUnit, data.buffList, ctx.buffManager, data);
                ApplyHealOptions(data, ctx.self.position, ctx.hero.Heal, AllyQuery, baseDamage, ctx.sc[StatType.ATK]);
            }
            ctx.hero.SpawnEffect(data.hitEffect, go.transform.position, Quaternion.identity, data.hitEffectLifetime);
        }, data.shotInterval, ct);
    }

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

        RangeShape aoeShape = data.areaShape == AreaShape.Square ? RangeShape.Square : RangeShape.Diamond;
        foreach (GameObject go in ctx.hero.GetObjectsInRange(ctx.self.position, data.areaRange, aoeShape, RangeQueryAffinity.Ally))
            if (go.GetComponent<Hero>() is Hero areaAlly)
                areaAlly.Heal(healAmount);
        ctx.hero.SpawnEffect(data.hitEffect, ctx.self.position, Quaternion.identity, data.hitEffectLifetime);

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
            if (effect.isTargetToOther) continue;
            buffManager.ApplyStackingModifier(selfUnit, effect.statType, effect.modifierType,
                effect.value, effect.duration, effect.maxStacks, source);
        }
    }

    public static void ApplyTargetDebuffs(IUnit target, List<BuffEffect> buffList, BuffManager buffManager, object source)
    {
        if (target == null || buffList == null) return;
        foreach (BuffEffect effect in buffList)
        {
            if (!effect.isTargetToOther) continue;
            buffManager.ApplyStackingModifier(target, effect.statType, effect.modifierType,
                effect.value, effect.duration, effect.maxStacks, source);
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
