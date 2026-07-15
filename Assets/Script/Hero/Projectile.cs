using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 15f;
    [SerializeField] private float hitDistance = 0.3f;
    [SerializeField] private float maxLifetime = 5f;
    [SerializeField] private float turnSpeed = 720f;

    private Transform target;
    private float damage;
    private float elapsed;
    private IObjectPool<Projectile> pool;
    private System.Func<Vector3, int, bool, List<IDamageAble>> getEnemiesInRange;
    private AttackType attackType;
    private int aoeRange;
    private bool aoeSquare;

    public void Launch(Transform target, float damage, IObjectPool<Projectile> pool,
        System.Func<Vector3, int, bool, List<IDamageAble>> getEnemiesInRange,
        AttackType attackType, int aoeRange, bool aoeSquare)
    {
        this.target = target;
        this.damage = damage;
        this.pool = pool;
        this.getEnemiesInRange = getEnemiesInRange;
        this.attackType = attackType;
        this.aoeRange = aoeRange;
        this.aoeSquare = aoeSquare;
        elapsed = 0f;

        if (target != null && TryLook(target.position - transform.position, out Quaternion look))
            transform.rotation = look;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= maxLifetime || target == null)
        {
            Return();
            return;
        }

        Vector3 toTarget = target.position - transform.position;
        if (toTarget.sqrMagnitude <= hitDistance * hitDistance)
        {
            Hit();
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
        if (attackType == AttackType.Splash)
        {
            foreach (IDamageAble enemy in getEnemiesInRange(transform.position, aoeRange, aoeSquare))
                enemy.TakeDamage((int)damage);
            SplashHighlighter.Instance?.Flash(transform.position, aoeRange, aoeSquare);
        }
        else if (target != null && target.GetComponentInParent<IDamageAble>() is IDamageAble damageable)
        {
            damageable.TakeDamage((int)damage);
        }
        Return();
    }

    private void Return()
    {
        if (pool != null) pool.Release(this);
        else Destroy(gameObject);
    }
}
