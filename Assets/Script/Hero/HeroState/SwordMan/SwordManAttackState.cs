using Cysharp.Threading.Tasks;
using UnityEngine;

public class SwordManAttackState : HeroAttackState
{
    private int currentAttackCount;
    private int attackCount = 3;
    private SwordMan swordMan;
    public SwordManAttackState(SwordMan swordMan, HeroStateMachine stateMachine) : base(swordMan, stateMachine)
    {
        currentAttackCount = 0;
        this.swordMan = swordMan;
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
