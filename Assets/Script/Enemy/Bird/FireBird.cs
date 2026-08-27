using UnityEngine;

public class FireBird : EnemyBase
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
