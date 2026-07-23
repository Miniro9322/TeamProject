using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class Mage : Hero
{
    [SerializeField] private Transform muzzle;
    public Transform Muzzle => muzzle;

    private readonly Dictionary<Projectile, IObjectPool<Projectile>> projectilePools = new();

    private IObjectPool<Projectile> GetProjectilePool(Projectile prefab)
    {
        if (!projectilePools.TryGetValue(prefab, out var pool))
        {
            pool = new ObjectPool<Projectile>(
                createFunc: () => Instantiate(prefab),
                actionOnGet: p => p.gameObject.SetActive(true),
                actionOnRelease: p => p.gameObject.SetActive(false),
                actionOnDestroy: p => Destroy(p.gameObject),
                collectionCheck: true,
                defaultCapacity: 8,
                maxSize: 32);
            projectilePools[prefab] = pool;
        }
        return pool;
    }

    protected override void Awake()
    {
        base.Awake();

        attackState = new MageAttackState(this, stateMachine);
        context = new AttackContext
        {
            self = transform,
            target = null,
            anim = Anim,
            animEvents = AnimEvents,
            muzzle = muzzle,
            getProjectilePool = GetProjectilePool,
            getEnemiesInRange = GetEnemiesInRange,
            getEnemyTargetsInRange = GetEnemyTransformsInRange,
            getEnemyObjectsInRange = GetEnemyObjectsInRange,
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
        occupantKind = OccupantKind.RangedHero;
    }
}
