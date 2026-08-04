using UnityEngine;

// 계산 결과를 받아 불 피해 진행 순서를 조립한다. Scene에는 배선하지 않는다.
public class BurnClock : MonoBehaviour
{
    private const string ClockName = "[BurnClock]";

    private static BurnClock instance;

    private FireConfig fireConfig;

    // 도메인 리로드를 끈 플레이 모드에서 지난 세션의 인스턴스 참조를 끊는다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    internal static void Ensure(FireConfig config)
    {
        if (instance != null)
        {
            return;
        }

        instance = new GameObject(ClockName).AddComponent<BurnClock>();
        instance.fireConfig = config;
    }

    internal static void EnterFire(
        Tile fireTile,
        GameObject unitObject)
    {
        if (BurningList.TryGetBurning(unitObject, out BurningUnit burningUnit))
        {
            burningUnit.RegisterFireTile(fireTile);
            return;
        }

        IDamageAble damageTarget = unitObject.GetComponentInParent<IDamageAble>();
        float firstDamageTime = BurnTiming.CalculateFirstDamageTime(Time.time);
        BurningUnit newBurningUnit = new(
            unitObject,
            damageTarget,
            fireTile,
            firstDamageTime);

        BurningList.AddLast(newBurningUnit);
    }

    internal static void ExitFire(
        Tile fireTile,
        GameObject unitObject)
    {
        if (!BurningList.TryGetBurning(unitObject, out BurningUnit burningUnit))
        {
            return;
        }

        burningUnit.RemoveFireTile(fireTile);

        if (burningUnit.HasActiveFireTile)
        {
            return;
        }

        float burnExpirationTime = BurnTiming.CalculateBurnExpirationTime(
            Time.time,
            instance.fireConfig.BurnDuration);

        burningUnit.ApplyBurnExpirationTime(burnExpirationTime);
    }

    private void Update()
    {
        if (BurningList.IsEmpty)
        {
            return;
        }

        BurningUnit burningUnit = BurningList.FirstBurning;

        if (burningUnit.IsUnavailable)
        {
            BurningList.RemoveFirst();
            return;
        }

        float currentTime = Time.time;
        bool isBurnExpired = BurnTiming.IsBurnExpirationTimeReached(
            currentTime,
            burningUnit.BurnExpirationTime,
            burningUnit.HasActiveFireTile);

        if (isBurnExpired)
        {
            BurningList.RemoveFirst();
            return;
        }

        bool isDamageTimeReached = BurnTiming.IsDamageTimeReached(
            currentTime,
            burningUnit.NextDamageTime);

        if (!isDamageTimeReached)
        {
            BurningList.SendFirstToBack();
            return;
        }

        float nextDamageTime = BurnTiming.CalculateNextDamageTime(
            currentTime,
            fireConfig.HitInterval);

        burningUnit.ApplyNextDamageTime(nextDamageTime);
        FireDamage.ApplyDamage(
            burningUnit.DamageTarget,
            fireConfig.DamagePerHit);
        BurningList.SendFirstToBack();
    }
}
