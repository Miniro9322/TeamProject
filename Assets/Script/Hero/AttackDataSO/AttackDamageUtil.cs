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

        // Line/Chain은 targetMode와 무관하게 자체 타겟팅 모델로 처리한다.
        if (data.attackType == AttackType.Area && data.areaShape == AreaShape.Line)
        {
            foreach (IDamageAble e in ctx.getEnemiesInLine(ctx.self.position, ctx.target.position, data.lineLength))
                e.TakeDamage((int)baseDamage);
            return;
        }

        if (data.attackType == AttackType.Area && data.areaShape == AreaShape.Chain)
        {
            ChainResolver.Resolve(ctx.target.gameObject, baseDamage, data.chainRange, data.chainCount,
                data.chainFalloff, ctx.getEnemyObjectsInRange);
            return;
        }

        if (data.attackType == AttackType.Single && data.targetMode == TargetMode.SameTarget)
        {
            if (ctx.target.GetComponent<IDamageAble>() is IDamageAble d)
                d.TakeDamage((int)baseDamage);
            return;
        }

        if (data.attackType == AttackType.Single && data.targetMode == TargetMode.DifferentEnemies)
        {
            List<IDamageAble> enemies = ctx.getEnemiesInRange(ctx.self.position, data.range, data.rangeShape);
            List<IDamageAble> targets = AttackTargetSelector.SelectTargets(enemies, data.attackCount, data.targetCount);
            await FireEach(targets, t => t.TakeDamage((int)baseDamage), data.shotInterval, ct);
            return;
        }

        RangeShape aoeShape = data.areaShape == AreaShape.Square ? RangeShape.Square : RangeShape.Diamond;

        if (data.attackType == AttackType.Area && data.targetMode == TargetMode.SameTarget)
        {
            for (int i = 0; i < data.attackCount; i++)
            {
                foreach (IDamageAble e in ctx.getEnemiesInRange(ctx.self.position, data.areaRange, aoeShape))
                    e.TakeDamage((int)baseDamage);
                SplashHighlighter.Instance?.Flash(ctx.self.position, data.areaRange, aoeShape);
                if (i < data.attackCount - 1)
                    await UniTask.Delay(TimeSpan.FromSeconds(data.shotInterval), cancellationToken: ct);
            }
            return;
        }

        // Area + DifferentEnemies: 서로 다른 적 최대 targetCount명, 각각을 중심으로 AOE 캐스트.
        List<GameObject> enemyObjects = ctx.getEnemyObjectsInRange(ctx.self.position, data.range, data.rangeShape);
        List<GameObject> centers = AttackTargetSelector.SelectTargets(enemyObjects, data.attackCount, data.targetCount);
        await FireEach(centers, go =>
        {
            foreach (IDamageAble e in ctx.getEnemiesInRange(go.transform.position, data.areaRange, aoeShape))
                e.TakeDamage((int)baseDamage);
            SplashHighlighter.Instance?.Flash(go.transform.position, data.areaRange, aoeShape);
        }, data.shotInterval, ct);
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
