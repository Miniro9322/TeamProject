using UnityEngine;

public class Spore : EnemyBase
{
    public override void DieSound()
    {
        base.DieSound();
        EnemySoundManager.Play("SporeDie");
    }
    public override void EnemySoundAttack()
    {
        base.EnemySoundAttack();
        EnemySoundManager.Play("SporeAttack");
    }
}
