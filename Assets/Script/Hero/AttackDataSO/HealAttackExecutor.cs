using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

// MeleeAttackExecutor와 동일한 애니메이션/히트 루프 구조지만, ApplyInstantDamage 대신
// ApplyInstantHeal을 호출해 아군을 회복시킨다(Healer 전용).
public class HealAttackExecutor : IAttackExecutor
{
    private readonly Dictionary<AttackDataSO, int> sequentialIndices = new();

    public async UniTask Execute(AttackDataSO data, AttackContext ctx, CancellationToken ct)
    {
        float interval = ctx.sc[StatType.AS] > 0f ? 1f / ctx.sc[StatType.AS] : 1f; // AS = 초당 공격 횟수
        AttackAnimSpeedUtil.SetSpeed(ctx.anim, AttackAnimSpeedUtil.ComputeScale(data, interval));

        ctx.anim.SetTrigger(PickTrigger(data));

        // 힐 장판은 대상보다 시전자(힐러) 발밑에 까는 편이 자연스럽다.
        if (data.groundZone != null)
            AttackDamageUtil.SpawnGroundZone(data.groundZone, ctx.self.position,
                ctx.getEnemyObjectsInRange, ctx.getAllyObjectsInRange, ctx.sc, ctx.buffManager, ct);

        try
        {
            float windowDuration = AttackAnimSpeedUtil.ComputeWindowDuration(data, interval);
            using var window = new AttackEventWindow(ctx.animEvents, "Attack", "Recovery", windowDuration);
            int hits = 0;
            while (await window.MoveNextHit(ct))
            {
                await AttackDamageUtil.ApplyInstantHeal(data, ctx, ct);
                hits++;
            }
            if (hits == 0)
                await AttackDamageUtil.ApplyInstantHeal(data, ctx, ct);
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
            Debug.LogError($"[HealAttackExecutor] '{data.name}' 의 animTriggers가 비어 있습니다.");
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
