using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class ChanceHeavyTrait : HeroTrait
{
    public float chance = 0.25f;
    public AttackDataSO heavyAttackData;

    public override void OnAttackPerformed(AttackDataSO data)
    {
        if (Random.value < chance)
        {
            hero.QueueNextAttackOverride(heavyAttackData);
        }
    }
}