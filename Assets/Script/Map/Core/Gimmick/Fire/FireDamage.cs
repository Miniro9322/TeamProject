// 불타는 유닛을 실제로 때리는 곳. 때릴 차례인지는 이미 가려져서 들어온다.
public static class FireDamage
{
    // 줄 맨 앞 유닛을 한 대 때리고 줄 맨 뒤로 보낸다.
    public static void StrikeFirst(int damagePerHit, float hitInterval)
    {
        BurningUnit burning = BurningList.FirstBurning;

        burning.DamageTarget.TakeDamage(damagePerHit);
        burning.NextHitTime = BurnTiming.NextHitTime(hitInterval);
        BurningList.SendFirstToBack();
    }
}
