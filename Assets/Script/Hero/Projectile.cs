using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;

public struct ProjectileAoEConfig
{
    public AttackType attackType;
    public AreaShape areaShape;
    public int areaRange;
    public int chainRange;
    public int chainCount;
    public float chainFalloff;
    public System.Func<Vector3, int, RangeShape, List<IDamageAble>> getEnemiesInRange;
    public System.Func<Vector3, int, RangeShape, List<GameObject>> getEnemyObjectsInRange;
    public System.Func<Vector3, int, RangeShape, List<GameObject>> getTargetableEnemyObjectsInRange;
    public System.Func<Vector3, int, RangeShape, List<GameObject>> getAllyObjectsInRange;
    public System.Action<float> healSelf;
    public List<BuffEffect> buffList;
    public BuffManager buffManager;
    public object source; // 보통 발사한 AttackDataSO 인스턴스
    public GroundZoneDataSO groundZone;
    public StatContainer attackerStats;
    public Vector3 casterPos; // 피흡/아군 힐 대상 조회 중심 — 착탄 지점(transform.position)이 아니라 발사자 기준이어야 한다.
    public System.Func<GameObject, Vector3, Quaternion, float, GameObject> spawnEffect;
    public System.Func<GameObject, Vector3, Quaternion, GameObject> spawnPersistentEffect;
    public System.Action<GameObject, GameObject> despawnEffect;
}

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 15f;
    [SerializeField] private float hitDistance = 0.3f;
    [SerializeField] private float maxLifetime = 5f;
    [SerializeField] private float turnSpeed = 720f;

    private Transform target;
    private Vector3 destination;
    private bool visualOnly;
    private float damage;
    private float elapsed;
    private IObjectPool<Projectile> pool;
    private ProjectileAoEConfig cfg;

    public void Launch(Transform target, float damage, IObjectPool<Projectile> pool, ProjectileAoEConfig cfg)
    {
        this.target = target;
        this.damage = damage;
        this.pool = pool;
        this.cfg = cfg;
        this.visualOnly = false;
        elapsed = 0f;

        if (target != null && TryLook(target.position - transform.position, out Quaternion look))
            transform.rotation = look;
    }

    // 관통(Line) 화살: 피해는 발사 시점에 이미 적용됐으므로, 명중 판정 없이 destination까지 날아가 사라진다.
    public void LaunchVisualOnly(Vector3 destination, IObjectPool<Projectile> pool)
    {
        this.destination = destination;
        this.pool = pool;
        this.visualOnly = true;
        this.target = null;
        elapsed = 0f;

        if (TryLook(destination - transform.position, out Quaternion look))
            transform.rotation = look;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= maxLifetime || (!visualOnly && target == null))
        {
            Return();
            return;
        }

        Vector3 dest = visualOnly ? destination : target.position;
        Vector3 toTarget = dest - transform.position;
        if (toTarget.sqrMagnitude <= hitDistance * hitDistance)
        {
            if (visualOnly) Return();
            else Hit();
            return;
        }

        if (TryLook(toTarget, out Quaternion desired))
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, turnSpeed * Time.deltaTime);

        transform.position += transform.forward * speed * Time.deltaTime;
    }

    // 방향이 유효할 때만 회전 산출. 위쪽 축과 거의 평행하면 대체 up으로 LookRotation 특이점 회피.
    private static bool TryLook(Vector3 dir, out Quaternion rot)
    {
        rot = Quaternion.identity;
        if (dir.sqrMagnitude < 1e-6f) return false;
        Vector3 up = Mathf.Abs(Vector3.Dot(dir.normalized, Vector3.up)) > 0.999f
            ? Vector3.forward : Vector3.up;
        rot = Quaternion.LookRotation(dir, up);
        return true;
    }

    private void Hit()
    {
        RangeShape aoeShape = cfg.areaShape == AreaShape.Square ? RangeShape.Square : RangeShape.Diamond;

        if (cfg.attackType == AttackType.Area && cfg.areaShape == AreaShape.Chain)
        {
            List<GameObject> hits = ChainResolver.Resolve(target.gameObject, damage, cfg.chainRange, cfg.chainCount, cfg.chainFalloff, cfg.getTargetableEnemyObjectsInRange);
            foreach (GameObject go in hits)
            {
                AttackDamageUtil.ApplyTargetDebuffs(go.GetComponentInParent<IUnit>(), cfg.buffList, cfg.buffManager, cfg.source);
                ApplyHealOptions(damage);
            }
            SpawnHitEffect();
        }
        else if (cfg.attackType == AttackType.Area)
        {
            foreach (IDamageAble enemy in cfg.getEnemiesInRange(transform.position, cfg.areaRange, aoeShape))
            {
                enemy.TakeDamage((int)damage);
                AttackDamageUtil.ApplyTargetDebuffs(enemy as IUnit, cfg.buffList, cfg.buffManager, cfg.source);
                ApplyHealOptions(damage);
            }
            SpawnHitEffect();
            //SplashHighlighter.Instance?.Flash(transform.position, cfg.areaRange, aoeShape);
        }
        else if (target != null && target.GetComponentInParent<IDamageAble>() is IDamageAble damageable)
        {
            damageable.TakeDamage((int)damage);
            AttackDamageUtil.ApplyTargetDebuffs(target.GetComponentInParent<IUnit>(), cfg.buffList, cfg.buffManager, cfg.source);
            ApplyHealOptions(damage);
            SpawnHitEffect();
        }

        AttackDamageUtil.SpawnGroundZone(cfg.groundZone, transform.position,
            cfg.getEnemyObjectsInRange, cfg.getAllyObjectsInRange, cfg.attackerStats, cfg.buffManager,
            cfg.spawnEffect, cfg.spawnPersistentEffect, cfg.despawnEffect, CancellationToken.None);

        Return();
    }

    // cfg.source는 발사한 AttackDataSO 인스턴스 — 그걸로 착탄 지점(transform.position)의 hitEffect를 스폰한다.
    private void SpawnHitEffect()
    {
        if (cfg.source is AttackDataSO data)
            cfg.spawnEffect(data.hitEffect, transform.position, Quaternion.identity, data.hitEffectLifetime);
    }

    // cfg.source는 발사한 AttackDataSO 인스턴스 — 그걸로 피흡/아군 힐 옵션을 조회해 적용한다.
    private void ApplyHealOptions(float damageDealt)
    {
        if (cfg.source is AttackDataSO data)
            AttackDamageUtil.ApplyHealOptions(data, cfg.casterPos, cfg.healSelf, cfg.getAllyObjectsInRange, damageDealt, cfg.attackerStats[StatType.ATK]);
    }

    private void Return()
    {
        if (pool != null) pool.Release(this);
        else Destroy(gameObject);
    }
}
