using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;

public class RangedAttackExecutor : IAttackExecutor
{
    private readonly Dictionary<AttackDataSO, int> sequentialIndices = new();

    public async UniTask Execute(AttackDataSO data, AttackContext ctx, CancellationToken ct)
    {
        float interval = ctx.sc[StatType.AS] > 0f ? 1f / ctx.sc[StatType.AS] : 1f;
        float scale = AttackAnimSpeedUtil.ComputeScale(data, interval);
        AttackAnimSpeedUtil.SetSpeed(ctx.anim, scale);
        if (ctx.bowAnim != null && ctx.arrowAnim != null)
        {
            AttackAnimSpeedUtil.SetSpeed(ctx.bowAnim, scale);
            AttackAnimSpeedUtil.SetSpeed(ctx.arrowAnim, scale);
        }

        string trigger = PickTrigger(data);
        ctx.anim.SetTrigger(trigger);

        if (ctx.bowAnim != null && ctx.arrowAnim != null)
        {
            ctx.bowAnim.SetTrigger("Attack");
            ctx.arrowAnim.SetTrigger("Attack");
        }

        IObjectPool<Projectile> pool = ctx.getProjectilePool(data.projectilePrefab);
        int damage = (int)(ctx.sc[StatType.ATK] * data.attackPer);

        try
        {
            float windowDuration = AttackAnimSpeedUtil.ComputeWindowDuration(data, interval);
            using var window = new AttackEventWindow(ctx.animEvents, "Attack", "Recovery", windowDuration);
            int hits = 0;
            while (await window.MoveNextHit(ct))
            {
                AttackDamageUtil.ApplySelfBuffs(ctx.selfUnit, data.buffList, ctx.buffManager, data);
                await FireVolley(data, ctx, pool, damage, ct);
                hits++;
            }
            if (hits == 0)
            {
                AttackDamageUtil.ApplySelfBuffs(ctx.selfUnit, data.buffList, ctx.buffManager, data);
                await FireVolley(data, ctx, pool, damage, ct);
            }
        }
        finally
        {
            AttackAnimSpeedUtil.SetSpeed(ctx.anim, 1f);
            if (ctx.bowAnim != null && ctx.arrowAnim != null)
            {
                AttackAnimSpeedUtil.SetSpeed(ctx.bowAnim, 1f);
                AttackAnimSpeedUtil.SetSpeed(ctx.arrowAnim, 1f);
            }
        }
    }

    private async UniTask FireVolley(AttackDataSO data, AttackContext ctx, IObjectPool<Projectile> pool, int damage, CancellationToken ct)
    {
        List<GameObject> targets;
        if (data.targetMode == TargetMode.DifferentEnemies)
        {
            List<GameObject> enemies = ctx.getObjectsInRange(ctx.self.position, data.range, data.rangeShape, RangeQueryAffinity.TargetableEnemy);
            targets = AttackTargetSelector.SelectTargets(enemies, data.attackCount, data.targetCount);
        }
        else
        {
            if (ctx.target == null) return; // 채널링/취소 경합 등으로 타겟이 비는 순간 방어
            targets = new List<GameObject>(data.attackCount);
            for (int i = 0; i < data.attackCount; i++)
                targets.Add(ctx.target.gameObject);
        }

        for (int i = 0; i < targets.Count; i++)
        {
            FireArrow(pool, ctx, targets[i].transform, damage, data);
            if (i < targets.Count - 1)
                await UniTask.Delay(System.TimeSpan.FromSeconds(data.shotInterval), cancellationToken: ct);
        }
    }

    private void FireArrow(IObjectPool<Projectile> pool, AttackContext ctx, Transform target, int damage, AttackDataSO data)
    {
        Projectile arrow = pool.Get();
        arrow.transform.SetPositionAndRotation(ctx.muzzle.position, ctx.muzzle.rotation);
        ctx.spawnEffect(data.attackEffect, ctx.muzzle.position, ctx.muzzle.rotation, data.attackEffectLifetime);

        if (data.attackType == AttackType.Area && data.areaShape == AreaShape.Line)
        {
            Vector2Int dir = ctx.getCardinalDirection(ctx.self.position, target.position);
            foreach (IDamageAble e in ctx.getEnemiesInLine(ctx.self.position, target.position, data.lineLength))
            {
                e.TakeDamage(damage);
                ctx.onHit?.Invoke((e as Component)?.gameObject, damage, false);
                AttackDamageUtil.ApplyTargetDebuffs(e as IUnit, data.buffList, ctx.buffManager, data);
                AttackDamageUtil.ApplyHealOptions(data, ctx.self.position, ctx.healSelf,
                    (p, r, s) => ctx.getObjectsInRange(p, r, s, RangeQueryAffinity.Ally), damage, ctx.sc[StatType.ATK]);
            }
            ctx.spawnEffect(data.hitEffect, target.position, Quaternion.identity, data.hitEffectLifetime);
            if (data.groundZonePrefab != null)
                ctx.spawnGroundZone?.Invoke(data.groundZonePrefab, target.position);
            Vector3 endPoint = ctx.getLineEndPoint(ctx.self.position, dir, data.lineLength);
            arrow.LaunchVisualOnly(endPoint, pool);
            return;
        }

        var cfg = new ProjectileAoEConfig
        {
            attackType = data.attackType,
            areaShape = data.areaShape,
            areaRange = data.areaRange,
            chainRange = data.chainRange,
            chainCount = data.chainCount,
            chainFalloff = data.chainFalloff,
            getObjectsInRange = ctx.getObjectsInRange,
            healSelf = ctx.healSelf,
            casterPos = ctx.self.position,
            buffList = data.buffList,
            buffManager = ctx.buffManager,
            source = data,
            groundZonePrefab = data.groundZonePrefab,
            spawnGroundZone = ctx.spawnGroundZone,
            onHit = ctx.onHit,
            attackerStats = ctx.sc,
            spawnEffect = ctx.spawnEffect,
            spawnPersistentEffect = ctx.spawnPersistentEffect,
            despawnEffect = ctx.despawnEffect,
        };
        arrow.Launch(target, damage, pool, cfg);
    }

    private string PickTrigger(AttackDataSO data)
    {
        var triggers = data.animTriggers;
        if (triggers == null || triggers.Length == 0)
        {
            Debug.LogError($"[RangedAttackExecutor] '{data.name}' 의 animTriggers가 비어 있습니다.");
            return string.Empty;
        }
        if (triggers.Length == 1) return triggers[0];

        if (data.selectMode == AnimSelectMode.Random)
            return triggers[Random.Range(0, triggers.Length)];

        sequentialIndices.TryGetValue(data, out int idx);
        sequentialIndices[data] = (idx + 1) % triggers.Length;
        return triggers[idx];
    }
}
