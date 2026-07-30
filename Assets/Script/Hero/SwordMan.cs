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
            spawnEffect = SpawnEffect,
            spawnPersistentEffect = SpawnPersistentEffect,
            despawnEffect = DespawnEffect,
            getEnemiesInRange = GetEnemiesInRange,
            getEnemyTargetsInRange = GetEnemyTransformsInRange,
            getEnemyObjectsInRange = GetEnemyObjectsInRange,
            getAllyObjectsInRange = GetAllyObjectsInRange,
            healSelf = amount => Heal(amount),
            getTargetableEnemiesInRange = GetTargetableEnemiesInRange,
            getTargetableEnemyObjectsInRange = GetTargetableEnemyObjectsInRange,
            getTargetableEnemyTargetsInRange = GetTargetableEnemyTransformsInRange,
            getEnemiesInLine = GetEnemiesInLine,
            getCardinalDirection = GetCardinalDirection,
            getLineEndPoint = GetLineEndPoint,
            buffManager = buffManager,
            sc = SC,
            selfUnit = this
        };
        occupantKind = OccupantKind.MeleeHero;
    }
}
