using UnityEngine;

// 불 줄을 매 프레임 굴리는 부품. FireConfig가 만들어 두므로 씬에 미리 놓지 않는다.
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

    // 굴리는 부품을 하나만 세워 둔다. FireConfig가 여럿이어도 시계는 하나다.
    internal static void Ensure(FireConfig config)
    {
        if (instance != null)
        {
            return;
        }

        instance = new GameObject(ClockName).AddComponent<BurnClock>();
        instance.fireConfig = config;
    }

    // 맞을 차례인 유닛이 있는 동안만 돈다. 차례가 아니면 한 바퀴도 돌지 않는다.
    private void Update()
    {
        while (BurnTiming.HasDueUnit())
        {
            FireDamage.StrikeFirst(fireConfig.DamagePerHit, fireConfig.HitInterval);
        }
    }
}
