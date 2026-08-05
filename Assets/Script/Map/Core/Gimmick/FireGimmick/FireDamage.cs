// 확정된 대상에게 확정된 불 피해만 적용한다.
public static class FireDamage
{
    public static void ApplyDamage(
        IDamageAble damageTarget,
        int damagePerHit)
    {
        damageTarget.TakeDamage(damagePerHit);
    }
}
