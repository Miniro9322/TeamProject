using UnityEngine;

public class HeroDeathState : HeroState
{
    public HeroDeathState(Hero hero, HeroStateMachine stateMachine) : base(hero, stateMachine)
    {
    }

    public override void Enter()
    {
        hero.Anim.SetBool(HeroAnimHash.death, true);
    }

    public override void Exit()
    {
        hero.Anim.SetBool(HeroAnimHash.death, false);
        stateMachine.ChangeState(hero.IdleState);
    }

    public override void Update()
    {
        
    }
}
