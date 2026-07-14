using Cysharp.Threading.Tasks;
using UnityEngine;

public class ArcherAttackState : HeroAttackState
{
    private Archer archer;
    private HeroAttackRunner runner;

    public ArcherAttackState(Archer archer, HeroStateMachine stateMachine)
        : base(archer, stateMachine)
    {
        this.archer = archer;
    }

    public override void Enter()
    {
        base.Enter();
        runner = new HeroAttackRunner(archer.BasePattern, archer.Selectors, archer.Procs, new RangedAttackExecutor());

        float cooldown = archer.SC[StatType.AS];
        float elapsed = Time.time - lastAttackTime;
        if (elapsed >= cooldown)
            TryExecuteCurrentStep();
        else
            timer = elapsed;
    }

    public override void Update()
    {
        base.Update();
        if (stateMachine.CurrentState != this) return;
        timer += Time.deltaTime;
        if (timer >= archer.SC[StatType.AS])
        {
            timer = 0f;
            TryExecuteCurrentStep();
        }
    }

    protected override void TryExecuteCurrentStep()
    {
        if (runner.IsExecuting) return;
        lastAttackTime = Time.time;
        runner.ExecuteNext(archer.Context, attackCts.Token).Forget();
    }
}
