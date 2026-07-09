using UnityEngine;

public class HeroAttackState : HeroState
{
    private float attackSpeed;
    private float timer;
    public HeroAttackState(Hero hero, HeroStateMachine stateMachine) : base(hero, stateMachine)
    {
        attackSpeed = hero.AttackSpeed;
    }

    public override void Enter()
    {
        timer = 0f;
        Attack();
        hero.Anim.SetTrigger(HeroAnimHash.attack);
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
            Attack();
            hero.Anim.SetTrigger(HeroAnimHash.attack);
        }
    }

    private void Attack()
    {
        Debug.Log("Attack!!");
    }
}
