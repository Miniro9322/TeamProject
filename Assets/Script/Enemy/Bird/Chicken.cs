using UnityEngine;

public class Chicken : EnemyBase
{
    public override void EnemySoundAttack()
    {
        base.EnemySoundAttack();
        EnemySoundManager.Play("SpiderAttack");

    }
    public override void DieSound()
    {
        base.DieSound();
        EnemySoundManager.Play("FlyDie");
    }
}
