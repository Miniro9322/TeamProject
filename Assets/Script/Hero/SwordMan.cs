using UnityEngine;

public class SwordMan : Hero
{
    protected override void Awake()
    {
        base.Awake();
        attackState = new SwordManAttackState(this, stateMachine);
        context = new AttackContext
        {
            self = transform,
            target = null,
            anim = Anim,
            animEvents = AnimEvents,
            getEnemiesInRange = GetEnemiesInRange,
            getEnemyTargetsInRange = GetEnemyTransformsInRange,
            getEnemyObjectsInRange = GetEnemyObjectsInRange,
            getEnemiesInLine = GetEnemiesInLine,
            getCardinalDirection = GetCardinalDirection,
            getLineEndPoint = GetLineEndPoint,
            buffManager = buffManager,
            sc = SC
        };
        occupantKind = OccupantKind.MeleeHero;
    }
}
