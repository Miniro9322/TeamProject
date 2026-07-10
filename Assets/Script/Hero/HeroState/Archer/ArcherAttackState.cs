using Cysharp.Threading.Tasks;
using UnityEngine;

public class ArcherAttackState : HeroAttackState
{
    private Archer archer;
    public ArcherAttackState(Archer archer, HeroStateMachine stateMachine) : base(archer, stateMachine)
    {
        this.archer = archer;
    }

    public override void Update()
    {
        base.Update();
        timer += Time.deltaTime;
        if (timer >= hero.AttackData.attackSpeed)
        {
            timer = 0f;
            hero.AttackData.Execute(hero.Context, attackCts.Token).Forget();
        }
    }
}
