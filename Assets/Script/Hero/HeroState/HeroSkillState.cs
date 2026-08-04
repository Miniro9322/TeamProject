using System;
using System.Threading;
using Cysharp.Threading.Tasks;

// activeSkill.blocksBasicAttack==true일 때 Hero.TryUseActiveSkill이 진입시키는 상태. 스킬의 Execute가
// 끝날 때까지 HeroIdleState/HeroAttackState.Update가 돌지 않아 기본공격이 자연스럽게 멈춘다.
// 종료되면 target 유무에 따라 Attack/Idle로 돌아간다.
public class HeroSkillState : HeroState
{
    private readonly Tile targetTile;
    private CancellationTokenSource cts;
    private bool finished;

    public HeroSkillState(Hero hero, HeroStateMachine stateMachine, Tile targetTile) : base(hero, stateMachine)
    {
        this.targetTile = targetTile;
    }

    public override void Enter()
    {
        finished = false;
        cts = new CancellationTokenSource();
        Run().Forget();
    }

    private async UniTask Run()
    {
        try { await hero.ActiveSkill.Execute(targetTile, cts.Token); }
        catch (OperationCanceledException) { }
        finally { finished = true; }
    }

    public override void Update()
    {
        if (finished)
            stateMachine.ChangeState(hero.Target != null ? hero.AttackState : hero.IdleState);
    }

    public override void Exit()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
    }
}
