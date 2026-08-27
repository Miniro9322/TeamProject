using UnityEngine;

public class Wraith : EnemyBase
{
    public override void EnemySoundAttack()
    {
        base.EnemySoundAttack();
        EnemySoundManager.Play("RushAttackHit");
    }
    public override void DieSound()
    {
        base.DieSound();
        EnemySoundManager.Play("GhostDie");
    }
}
