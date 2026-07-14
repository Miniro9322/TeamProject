using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 15f;
    [SerializeField] private float hitDistance = 0.3f;
    [SerializeField] private float maxLifetime = 5f;

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

        transform.rotation = Quaternion.LookRotation(toTarget);
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    private void Hit()
    {
        if (attackType == AttackType.Multiple)
        {
            foreach (IDamageAble enemy in getEnemiesInRange(transform.position, aoeRange, aoeSquare))
                enemy.TakeDamage((int)damage);
        }
        else if (target != null && target.GetComponent<IDamageAble>() is IDamageAble damageable)
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
