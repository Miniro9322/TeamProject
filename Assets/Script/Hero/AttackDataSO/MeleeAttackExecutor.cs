using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class MeleeAttackExecutor : IAttackExecutor
{
    private readonly Dictionary<AttackDataSO, int> sequentialIndices = new();

    public async UniTask Execute(AttackDataSO data, AttackContext ctx, CancellationToken ct)
    {
        float interval = ctx.sc[StatType.AS] > 0f ? 1f / ctx.sc[StatType.AS] : 1f;
        AttackAnimSpeedUtil.SetSpeed(ctx.anim, AttackAnimSpeedUtil.ComputeScale(data, interval));

        ctx.anim.SetTrigger(PickTrigger(data));

        if (data.groundZonePrefab != null && ctx.target != null)
            ctx.spawnGroundZone?.Invoke(data.groundZonePrefab, ctx.target.position);

        try
        {
            float windowDuration = AttackAnimSpeedUtil.ComputeWindowDuration(data, interval);
            using var window = new AttackEventWindow(ctx.animEvents, "Attack", "Recovery", windowDuration);
            int hits = 0;
            while (await window.MoveNextHit(ct))
            {
                ctx.spawnEffect(data.attackEffect, ctx.self.position, ctx.self.rotation, data.attackEffectLifetime);
                await AttackDamageUtil.ApplyInstantDamage(data, ctx, ct);
                hits++;
            }
            // 고속 공격속도로 인해 애니메이터가 "Attack" 이벤트를 유실하면(재트리거/전이 도중) 타격이
            // 0회가 되어 데미지가 통째로 사라진다. window가 취소 없이 정상 종료됐다면 최소 1회는 보장 적용한다.
            if (hits == 0)
            {
                ctx.spawnEffect(data.attackEffect, ctx.self.position, ctx.self.rotation, data.attackEffectLifetime);
                await AttackDamageUtil.ApplyInstantDamage(data, ctx, ct);
            }
        }
        finally
        {
            AttackAnimSpeedUtil.SetSpeed(ctx.anim, 1f);
        }
    }

    private string PickTrigger(AttackDataSO data)
    {
        var triggers = data.animTriggers;
        if (triggers == null || triggers.Length == 0)
        {
            Debug.LogError($"[MeleeAttackExecutor] '{data.name}' 의 animTriggers가 비어 있습니다.");
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
