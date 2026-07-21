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
        float interval = ctx.sc[StatType.AS] > 0f ? 1f / ctx.sc[StatType.AS] : 1f; // AS = 초당 공격 횟수
        float scale = AttackAnimSpeedUtil.ComputeScale(data, interval);
        AttackAnimSpeedUtil.SetSpeed(ctx.anim, scale);
        AttackAnimSpeedUtil.SetSpeed(ctx.bowAnim, scale);
        AttackAnimSpeedUtil.SetSpeed(ctx.arrowAnim, scale);

        string trigger = PickTrigger(data);
        ctx.anim.SetTrigger(trigger);
        ctx.bowAnim.SetTrigger("Attack");
        ctx.arrowAnim.SetTrigger("Attack");

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
            // 고속 공격속도로 "Attack" 애니메이션 이벤트가 유실되면 발사가 0회가 될 수 있다.
            // window가 취소 없이 정상 종료됐다면 최소 1회는 보장 발사한다.
            if (hits == 0)
            {
                AttackDamageUtil.ApplySelfBuffs(ctx.selfUnit, data.buffList, ctx.buffManager, data);
                await FireVolley(data, ctx, pool, damage, ct);
            }
        }
        finally
        {
            AttackAnimSpeedUtil.SetSpeed(ctx.anim, 1f);
            AttackAnimSpeedUtil.SetSpeed(ctx.bowAnim, 1f);
            AttackAnimSpeedUtil.SetSpeed(ctx.arrowAnim, 1f);
        }
    }

    private async UniTask FireVolley(AttackDataSO data, AttackContext ctx, IObjectPool<Projectile> pool, int damage, CancellationToken ct)
    {
        List<Transform> targets;
        if (data.targetMode == TargetMode.DifferentEnemies)
        {
            List<Transform> enemies = ctx.getEnemyTargetsInRange(ctx.self.position, data.range, data.rangeShape);
            targets = AttackTargetSelector.SelectTargets(enemies, data.attackCount, data.targetCount);
        }
        else
        {
            targets = new List<Transform>(data.attackCount);
            for (int i = 0; i < data.attackCount; i++)
                targets.Add(ctx.target);
        }

        for (int i = 0; i < targets.Count; i++)
        {
            FireArrow(pool, ctx, targets[i], damage, data);
            if (i < targets.Count - 1)
                await UniTask.Delay(System.TimeSpan.FromSeconds(data.shotInterval), cancellationToken: ct);
        }
    }

    private void FireArrow(IObjectPool<Projectile> pool, AttackContext ctx, Transform target, int damage, AttackDataSO data)
    {
        Projectile arrow = pool.Get();
        arrow.transform.SetPositionAndRotation(ctx.muzzle.position, ctx.muzzle.rotation);

        if (data.attackType == AttackType.Area && data.areaShape == AreaShape.Line)
        {
            // 관통: 발사 즉시 라인상의 모든 적에게 피해를 적용하고, 화살은 시각 전용으로 라인 끝까지 날린다.
            Vector2Int dir = ctx.getCardinalDirection(ctx.self.position, target.position);
            foreach (IDamageAble e in ctx.getEnemiesInLine(ctx.self.position, target.position, data.lineLength))
            {
                e.TakeDamage(damage);
                AttackDamageUtil.ApplyTargetDebuffs(e as IUnit, data.buffList, ctx.buffManager, data);
            }
            AttackDamageUtil.SpawnGroundZone(data.groundZone, target.position,
                ctx.getEnemyObjectsInRange, ctx.sc, ctx.buffManager, CancellationToken.None);
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
            getEnemiesInRange = ctx.getEnemiesInRange,
            getEnemyObjectsInRange = ctx.getEnemyObjectsInRange,
            buffList = data.buffList,
            buffManager = ctx.buffManager,
            source = data,
            groundZone = data.groundZone,
            attackerStats = ctx.sc,
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
