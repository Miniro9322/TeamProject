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
    public List<BuffEffect> buffList;
    public BuffManager buffManager;
    public object source; // 보통 발사한 AttackDataSO 인스턴스
    public GameObject groundZonePrefab;
    public StatContainer attackerStats;
    public Vector3 casterPos;
    public Hero hero;
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
            List<GameObject> hits = ChainResolver.Resolve(target.gameObject, damage, cfg.chainRange, cfg.chainCount, cfg.chainFalloff,
                (p, r, s) => cfg.hero.GetObjectsInRange(p, r, s, RangeQueryAffinity.TargetableEnemy), cfg.hero.NotifyHit);
            foreach (GameObject go in hits)
            {
                AttackDamageUtil.ApplyTargetDebuffs(go.GetComponentInParent<IUnit>(), cfg.buffList, cfg.buffManager, cfg.source);
                ApplyHealOptions(damage);
            }
            SpawnHitEffect();
        }
        else if (cfg.attackType == AttackType.Area)
        {
            foreach (GameObject go in cfg.hero.GetObjectsInRange(transform.position, cfg.areaRange, aoeShape, RangeQueryAffinity.Enemy))
            {
                if (go.GetComponentInParent<IDamageAble>() is not IDamageAble enemy) continue;
                enemy.TakeDamage((int)damage);
                cfg.hero.NotifyHit(go, (int)damage, false);
                AttackDamageUtil.ApplyTargetDebuffs(enemy as IUnit, cfg.buffList, cfg.buffManager, cfg.source);
                ApplyHealOptions(damage);
            }
            SpawnHitEffect();
        }
        else if (target != null && target.GetComponentInParent<IDamageAble>() is IDamageAble damageable)
        {
            damageable.TakeDamage((int)damage);
            cfg.hero.NotifyHit(target.gameObject, (int)damage, false);
            AttackDamageUtil.ApplyTargetDebuffs(target.GetComponentInParent<IUnit>(), cfg.buffList, cfg.buffManager, cfg.source);
            ApplyHealOptions(damage);
            SpawnHitEffect();
        }

        if (cfg.groundZonePrefab != null)
            cfg.hero.SpawnGroundZone(cfg.groundZonePrefab, transform.position);

        Return();
    }

    private void SpawnHitEffect()
    {
        if (cfg.source is AttackDataSO data)
            cfg.hero.SpawnEffect(data.hitEffect, transform.position, Quaternion.identity, data.hitEffectLifetime);
    }

    private void ApplyHealOptions(float damageDealt)
    {
        if (cfg.source is AttackDataSO data)
            AttackDamageUtil.ApplyHealOptions(data, cfg.casterPos, cfg.hero.Heal,
                (p, r, s) => cfg.hero.GetObjectsInRange(p, r, s, RangeQueryAffinity.Ally), damageDealt, cfg.attackerStats[StatType.ATK]);
    }

    private void Return()
    {
        if (pool != null) pool.Release(this);
        else Destroy(gameObject);
    }
}
