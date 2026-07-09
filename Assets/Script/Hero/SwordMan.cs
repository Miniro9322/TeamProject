using UnityEngine;

public class SwordMan : Hero
{
    protected override void Awake()
    {
        base.Awake();
        attackState = new HeroAttackState(this, stateMachine);
    }
}
