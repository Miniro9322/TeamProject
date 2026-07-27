using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public static class AttackDamageUtil
{
    //public static int count = 0;
    public static async UniTask ApplyInstantDamage(AttackDataSO data, AttackContext ctx, CancellationToken ct)
    {
        float baseDamage = ctx.sc[StatType.ATK] * data.attackPer;
        
        ApplySelfBuffs(ctx.selfUnit, data.buffList, ctx.buffManager, data); // 공격 1회당 1회, 맞은 대상 수와 무관하게 적용(스택형 자기 버프용)
        //Debug.Log($"Damage: {++count}");
        // Line/Chain은 targetMode와 무관하게 자체 타겟팅 모델로 처리한다.
        if (data.attackType == AttackType.Area && data.areaShape == AreaShape.Line)
        {
            foreach (IDamageAble e in ctx.getEnemiesInLine(ctx.self.position, ctx.target.position, data.lineLength))
            {
                e.TakeDamage((int)baseDamage);
                ApplyTargetDebuffs(e as IUnit, data.buffList, ctx.buffManager, data);
                ApplyHealOptions(data, ctx.self.position, ctx.healSelf, ctx.getAllyObjectsInRange, baseDamage);
            }
            return;
        }

        if (data.attackType == AttackType.Area && data.areaShape == AreaShape.Chain)
        {
            List<GameObject> hits = ChainResolver.Resolve(ctx.target.gameObject, baseDamage, data.chainRange, data.chainCount,
                data.chainFalloff, ctx.getTargetableEnemyObjectsInRange);
            foreach (GameObject go in hits)
            {
                ApplyTargetDebuffs(go.GetComponentInParent<IUnit>(), data.buffList, ctx.buffManager, data);
                ApplyHealOptions(data, ctx.self.position, ctx.healSelf, ctx.getAllyObjectsInRange, baseDamage);
            }
            return;
        }

        if (data.attackType == AttackType.Single && data.targetMode == TargetMode.SameTarget)
        {
            if (ctx.target.GetComponent<IDamageAble>() is IDamageAble d)
            {
                d.TakeDamage((int)baseDamage);
                ApplyTargetDebuffs(d as IUnit, data.buffList, ctx.buffManager, data);
                ApplyHealOptions(data, ctx.self.position, ctx.healSelf, ctx.getAllyObjectsInRange, baseDamage);
            }
            return;
        }

        if (data.attackType == AttackType.Single && data.targetMode == TargetMode.DifferentEnemies)
        {
            List<IDamageAble> enemies = ctx.getTargetableEnemiesInRange(ctx.self.position, data.range, data.rangeShape);
            List<IDamageAble> targets = AttackTargetSelector.SelectTargets(enemies, data.attackCount, data.targetCount);
            await FireEach(targets, t =>
            {
                t.TakeDamage((int)baseDamage);
                ApplyTargetDebuffs(t as IUnit, data.buffList, ctx.buffManager, data);
                ApplyHealOptions(data, ctx.self.position, ctx.healSelf, ctx.getAllyObjectsInRange, baseDamage);
            }, data.shotInterval, ct);
            return;
        }

        RangeShape aoeShape = data.areaShape == AreaShape.Square ? RangeShape.Square : RangeShape.Diamond;

        if (data.attackType == AttackType.Area && data.targetMode == TargetMode.SameTarget)
        {
            for (int i = 0; i < data.attackCount; i++)
            {
                foreach (IDamageAble e in ctx.getEnemiesInRange(ctx.self.position, data.areaRange, aoeShape))
                {
                    e.TakeDamage((int)baseDamage);
                    ApplyTargetDebuffs(e as IUnit, data.buffList, ctx.buffManager, data);
                    ApplyHealOptions(data, ctx.self.position, ctx.healSelf, ctx.getAllyObjectsInRange, baseDamage);
                }
                //SplashHighlighter.Instance?.Flash(ctx.self.position, data.areaRange, aoeShape);
                if (i < data.attackCount - 1)
                    await UniTask.Delay(TimeSpan.FromSeconds(data.shotInterval), cancellationToken: ct);
            }
            return;
        }

        // Area + DifferentEnemies: 서로 다른 적 최대 targetCount명, 각각을 중심으로 AOE 캐스트.
        List<GameObject> enemyObjects = ctx.getTargetableEnemyObjectsInRange(ctx.self.position, data.range, data.rangeShape);
        List<GameObject> centers = AttackTargetSelector.SelectTargets(enemyObjects, data.attackCount, data.targetCount);
        await FireEach(centers, go =>
        {
            foreach (IDamageAble e in ctx.getEnemiesInRange(go.transform.position, data.areaRange, aoeShape))
            {
                e.TakeDamage((int)baseDamage);
                ApplyTargetDebuffs(e as IUnit, data.buffList, ctx.buffManager, data);
                ApplyHealOptions(data, ctx.self.position, ctx.healSelf, ctx.getAllyObjectsInRange, baseDamage);
            }
            //SplashHighlighter.Instance?.Flash(go.transform.position, data.areaRange, aoeShape);
        }, data.shotInterval, ct);
    }

    // 공격 1회 적중당 호출: 피흡(공격자 자가 회복)과 아군 힐(범위 내 최저 체력 아군 1명 회복) 옵션 적용.
    // 두 옵션 모두 기본값(0)이면 즉시 반환되므로 기존 공격 데이터에는 아무 영향이 없다.
    public static void ApplyHealOptions(AttackDataSO data, Vector3 selfPos,
        Action<float> healSelf,
        Func<Vector3, int, RangeShape, List<GameObject>> getAllyObjectsInRange,
        float damageDealt)
    {
        if (data.lifestealPercent > 0f && damageDealt > 0f)
            healSelf?.Invoke(damageDealt * data.lifestealPercent);

        if (data.allyHealAmount > 0f && getAllyObjectsInRange != null)
        {
            Hero target = FindLowestHpAlly(getAllyObjectsInRange(selfPos, data.allyHealRange, data.allyHealRangeShape));
            target?.Heal(data.allyHealAmount);
        }
    }

    // 후보 중 IDamageAble.Hp가 가장 낮은 대상을 찾는다. GroundZoneRunner의 힐 장판 틱에서도 재사용.
    public static Hero FindLowestHpAlly(List<GameObject> candidates)
    {
        Hero lowest = null;
        float lowestHp = float.MaxValue;
        foreach (GameObject go in candidates)
        {
            if (go.GetComponentInParent<Hero>() is Hero d && d.Hp < lowestHp)
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

    public static void SpawnGroundZone(GroundZoneDataSO zoneData, Vector3 center,
        Func<Vector3, int, RangeShape, List<GameObject>> getEnemyObjectsInRange,
        Func<Vector3, int, RangeShape, List<GameObject>> getAllyObjectsInRange,
        StatContainer attackerStats, BuffManager buffManager, CancellationToken ct)
    {
        if (zoneData == null) return;
        GroundZoneRunner.Run(center, zoneData, getEnemyObjectsInRange, getAllyObjectsInRange, attackerStats, buffManager, () => true, ct).Forget();
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
