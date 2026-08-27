using UnityEngine;

public class IceGolem : EnemyBase
{
    public override void EnemySoundAttack()
    {
        base.EnemySoundAttack();
        EnemySoundManager.Play("GolemAttack");
    }
    public override void DieSound()
    {
        base.DieSound();
        EnemySoundManager.Play("GolemDie");
    }
}
