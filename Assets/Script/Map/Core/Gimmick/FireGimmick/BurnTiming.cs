// 불 피해와 만료 시각을 계산한다. 상태는 바꾸지 않는다.
public static class BurnTiming
{
    public static float CalculateFirstDamageTime(float currentTime)
    {
        return currentTime;
    }

    public static float CalculateNextDamageTime(
        float currentTime,
        float damageInterval)
    {
        return currentTime + damageInterval;
    }

    public static float CalculateBurnExpirationTime(
        float currentTime,
        float burnDuration)
    {
        return currentTime + burnDuration;
    }

    public static bool IsDamageTimeReached(
        float currentTime,
        float nextDamageTime)
    {
        return currentTime >= nextDamageTime;
    }

    public static bool IsBurnExpirationTimeReached(
        float currentTime,
        float burnExpirationTime,
        bool hasActiveFireTile)
    {
        return !hasActiveFireTile && currentTime >= burnExpirationTime;
    }
}
