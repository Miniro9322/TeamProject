using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class HeroAttackState : HeroState
{
    protected float attackSpeed;
    protected float timer;
    protected CancellationTokenSource attackCts;
    public HeroAttackState(Hero hero, HeroStateMachine stateMachine) : base(hero, stateMachine)
    {
        attackSpeed = hero.AttackSpeed;
    }

    public override void Enter()
    {
        timer = 0f;
        attackCts = new CancellationTokenSource();
    }

    public override void Exit()
    {
        attackCts?.Cancel();
        attackCts?.Dispose();
        attackCts = null;
    }

    public override void Update()
    {
        if (hero.Target == null)
        {
            stateMachine.ChangeState(hero.IdleState);
            return;
        }
        Vector3 aimVector = hero.Target.transform.position - hero.transform.position;
        aimVector.y = 0f;

        if (aimVector.sqrMagnitude > 0.00001f)
            hero.transform.rotation = Quaternion.LookRotation(aimVector);

        timer += Time.deltaTime;
        if (timer >= attackSpeed)
        {
            timer = 0f;
            // 공격
            // hero.AttackData.Execute();
        }
    }
}
