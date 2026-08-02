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
            getObjectsInRange = GetObjectsInRange,
            healSelf = amount => Heal(amount),
            getEnemiesInLine = GetEnemiesInLine,
            getCardinalDirection = GetCardinalDirection,
            getLineEndPoint = GetLineEndPoint,
            buffManager = buffManager,
            sc = SC,
            selfUnit = this,
            onHit = NotifyHit,
            spawnGroundZone = SpawnGroundZone
        };
        occupantKind = OccupantKind.MeleeHero;
    }
}
