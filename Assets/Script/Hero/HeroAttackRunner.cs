using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

public class HeroAttackRunner
{
    private const int MaxProcDepth = 5;

    private readonly List<AttackDataSO> basePattern;
    private readonly List<AttackSelectorSO> selectors;
    private readonly List<AttackProcSO> procs;
    private readonly IAttackExecutor executor;

    private AttackDataSO pendingOverride;

    public int HitCount { get; private set; }
    public bool IsExecuting { get; private set; }

    public HeroAttackRunner(
        List<AttackDataSO> basePattern,
        List<AttackSelectorSO> selectors,
        List<AttackProcSO> procs,
        IAttackExecutor executor)
    {
        this.basePattern = basePattern;
        this.selectors = selectors;
        this.procs = procs;
        this.executor = executor;
    }

    public AttackDataSO ResolveNext(AttackContext ctx)
    {
        if (pendingOverride != null)
        {
            var forced = pendingOverride;
            pendingOverride = null;
            return forced;
        }
        foreach (var sel in selectors)
        {
            var result = sel.Select(HitCount, ctx);
            if (result != null) return result;
        }
        return basePattern[HitCount % basePattern.Count];
    }

    public async UniTask ExecuteNext(AttackContext ctx, CancellationToken ct)
    {
        if (IsExecuting) return;
        IsExecuting = true;
        try
        {
            var attack = ResolveNext(ctx);
            HitCount++;
            await ExecuteWithProcs(attack, ctx, ct, depth: 0);
        }
        finally
        {
            IsExecuting = false;
        }
    }

    private async UniTask ExecuteWithProcs(
        AttackDataSO attack, AttackContext ctx, CancellationToken ct, int depth)
    {
        if (depth >= MaxProcDepth) return;

        await executor.Execute(attack, ctx, ct);
        
        foreach (var proc in procs)
        {
            if (depth > 0 && !proc.allowRecursiveProc) continue;
            if (!proc.ShouldProc(attack, ctx)) continue;

            switch (proc.effectType)
            {
                case ProcEffectType.ExtraAttack:
                    await ExecuteWithProcs(proc.GetProcAttack(attack), ctx, ct, depth + 1);
                    break;
                case ProcEffectType.UpgradeNextAttack:
                    pendingOverride = proc.GetProcAttack(attack);
                    break;
                case ProcEffectType.BonusDamage:
                    AttackDamageUtil.ApplyInstantDamage(proc.GetProcAttack(attack), ctx, ct).Forget();
                    break;
            }
        }
    }
}
