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
    public List<TargetDebuffRef> targetDebuffs;
    public BuffManager buffManager;
    public object source; // 보통 발사한 AttackDataSO 인스턴스
    public GameObject groundZonePrefab;
    public EnemyAttribute areaUnattackableTarget;
    public StatContainer attackerStats;
    public Vector3 casterPos;
    public Hero hero;

    // RangedAttackExecutor.FireArrow와 ContinuousBeamStrategy.FireOneProjectile 두 곳이 이 구성을
    // 동일하게 만들고 있었다. 필드가 늘어날 때 한쪽만 고쳐 조용히 어긋나는 걸 막기 위해 유일한
    // 구성 지점으로 모은다. AttackContext를 그대로 받지 않는 이유: ContinuousBeamStrategy는 ctx가
    // 낡은 스냅샷일 수 있어 hero를 별도 인자로 받는다(ctx.hero를 쓰면 어느 쪽이 권위인지 흐려진다).
    public static ProjectileAoEConfig From(AttackDataSO data, Hero hero,
        StatContainer attackerStats, BuffManager buffManager, Vector3 casterPos) => new()
    {
        attackType = data.attackType,
        areaShape = data.areaShape,
        areaRange = data.areaRange,
        chainRange = data.chainRange,
        chainCount = data.chainCount,
        chainFalloff = data.chainFalloff,
        casterPos = casterPos,
        targetDebuffs = data.targetDebuffs,
        buffManager = buffManager,
        source = data,
        groundZonePrefab = data.groundZonePrefab,
        areaUnattackableTarget = data.AreaUnattackableTarget,
        attackerStats = attackerStats,
        hero = hero,
    };
}

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 15f;
    [SerializeField] private float hitDistance = 0.3f;
    [SerializeField] private float maxLifetime = 5f;
    [SerializeField] private float turnSpeed = 720f;

    [Header("이펙트 (프리팹 자체 소유 — AttackDataSO.attackEffect/hitEffect는 참조하지 않음)")]
    [SerializeField] private GameObject flashEffectPrefab;
    [SerializeField] private float flashEffectLifetime = 1f;
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private float hitEffectLifetime = 1f;
    [Tooltip("비행 내내 따라다니는 자식 파티클 — 부모 Transform을 자동으로 따라가므로 재배치 코드 불필요")]
    [SerializeField] private ParticleSystem projectileEffect;

    private Transform target;
    private Vector3 destination;
    private bool visualOnly;
    private float damage;
    private float elapsed;
    private IObjectPool<Projectile> pool;
    private ProjectileAoEConfig cfg;
    private Hero hero;

    public void Launch(Transform target, float damage, IObjectPool<Projectile> pool, ProjectileAoEConfig cfg)
    {
        this.target = target;
        this.damage = damage;
        this.pool = pool;
        this.cfg = cfg;
        this.hero = cfg.hero;
        this.visualOnly = false;
        elapsed = 0f;

        if (target != null && TryLook(target.position - transform.position, out Quaternion look))
            transform.rotation = look;

        SpawnFlashEffect(cfg.hero);
    }

    public void LaunchVisualOnly(Vector3 destination, IObjectPool<Projectile> pool, Hero hero)
    {
        this.destination = destination;
        this.pool = pool;
        this.hero = hero;
        this.visualOnly = true;
        this.target = null;
        elapsed = 0f;

        if (TryLook(destination - transform.position, out Quaternion look))
            transform.rotation = look;

        SpawnFlashEffect(hero);
    }

    private void SpawnFlashEffect(Hero hero)
    {
        if (flashEffectPrefab != null && hero != null)
            hero.SpawnEffect(flashEffectPrefab, transform.position, transform.rotation, flashEffectLifetime);
        if (projectileEffect != null)
        {
            projectileEffect.Clear(true);
            projectileEffect.Play(true);
        }
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= maxLifetime || (!visualOnly && target == null))
        {
            Return();
            return;
        }

        Vector3 dest = visualOnly ? destination : AttackDamageUtil.EffectPosition(target.gameObject);
        Vector3 oldPos = transform.position;
        Vector3 toTarget = dest - oldPos;

        if (TryLook(toTarget, out Quaternion desired))
            transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, turnSpeed * Time.deltaTime);

        Vector3 newPos = oldPos + transform.forward * speed * Time.deltaTime;

        // 고배속/프레임드랍으로 한 프레임 이동거리가 hitDistance를 넘으면 목표를 관통하거나
        // 옆으로 스쳐 지나갈 수 있다. 이번 프레임 이동 경로(선분) 기준으로 판정해 그런 경우도 잡는다.
        Vector3 closest = ClosestPointOnSegment(dest, oldPos, newPos);
        if ((dest - closest).sqrMagnitude <= hitDistance * hitDistance)
        {
            // Hit()의 범위 공격/장판 스폰이 transform.position을 기준으로 하므로,
            // 관통/스쳐 지나간 경우에도 실제 명중 지점(선분상 최근접점)으로 스냅해 둔다.
            transform.position = closest;
            if (visualOnly) { SpawnHitEffect(dest); Return(); }
            else Hit();
            return;
        }

        transform.position = newPos;
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

    private static Vector3 ClosestPointOnSegment(Vector3 point, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float abLenSqr = ab.sqrMagnitude;
        float t = abLenSqr > 1e-9f ? Mathf.Clamp01(Vector3.Dot(point - a, ab) / abLenSqr) : 0f;
        return a + ab * t;
    }

    private void Hit()
    {
        RangeShape aoeShape = cfg.areaShape == AreaShape.Square ? RangeShape.Square : RangeShape.Diamond;

        if (cfg.areaShape == AreaShape.Chain)
        {
            List<GameObject> hits = ChainResolver.Resolve(target.gameObject, damage, cfg.chainRange, cfg.chainCount, cfg.chainFalloff,
                (p, r, s) => cfg.hero.GetObjectsInRange(p, r, s, RangeQueryAffinity.TargetableEnemy), cfg.hero.NotifyHit);
            foreach (GameObject go in hits)
            {
                AttackDamageUtil.ApplyTargetDebuffs(go.transform, cfg.targetDebuffs, cfg.buffManager, cfg.source);
                ApplyHealOptions(damage);
                SpawnHitEffect(AttackDamageUtil.EffectPosition(go));
            }
        }
        else if (cfg.attackType == AttackType.Area)
        {
            foreach (GameObject go in cfg.hero.GetObjectsInRange(transform.position, cfg.areaRange, aoeShape, RangeQueryAffinity.Enemy, cfg.areaUnattackableTarget))
            {
                if (go.GetComponentInParent<IDamageAble>() is not IDamageAble enemy) continue;
                enemy.TakeDamage((int)damage);
                cfg.hero.NotifyHit(go, (int)damage, false);
                AttackDamageUtil.ApplyTargetDebuffs(go.transform, cfg.targetDebuffs, cfg.buffManager, cfg.source);
                ApplyHealOptions(damage);
                SpawnHitEffect(AttackDamageUtil.EffectPosition(go));
            }
        }
        else if (target != null && target.GetComponentInParent<IDamageAble>() is IDamageAble damageable)
        {
            damageable.TakeDamage((int)damage);
            cfg.hero.NotifyHit(target.gameObject, (int)damage, false);
            AttackDamageUtil.ApplyTargetDebuffs(target, cfg.targetDebuffs, cfg.buffManager, cfg.source);
            ApplyHealOptions(damage);
            SpawnHitEffect(AttackDamageUtil.EffectPosition(target));
        }

        if (cfg.groundZonePrefab != null)
            cfg.hero.SpawnGroundZone(cfg.groundZonePrefab, transform.position);

        Return();
    }

    private void SpawnHitEffect(Vector3 pos)
    {
        if (hitEffectPrefab != null)
            hero.SpawnEffect(hitEffectPrefab, pos, Quaternion.identity, hitEffectLifetime);
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
