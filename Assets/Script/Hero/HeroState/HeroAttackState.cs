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
        PlayAttack();
    }

    public override void Exit()
    {
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
            PlayAttack();
        }
    }

    protected virtual void PlayAttack()
    {
        attackCts?.Cancel();
        attackCts?.Dispose();
        attackCts = new CancellationTokenSource();
        hero.Anim.SetTrigger(HeroAnimHash.attack);
        Attack().Forget();
    }
    public async UniTask WaitForAnimEvent(string eventName, CancellationToken ct)
    {
        bool fired = false;
        System.Action handler = () => fired = true;
        hero.AnimEvents.Subscribe(eventName, handler);
        await UniTask.WaitUntil(() => fired, cancellationToken: ct);
        hero.AnimEvents.Unsubscribe(eventName, handler);
    }
    protected virtual async UniTask Attack()
    {
        await WaitForAnimEvent("Attack", attackCts.Token);
        Debug.Log("Attack");
    }
}
