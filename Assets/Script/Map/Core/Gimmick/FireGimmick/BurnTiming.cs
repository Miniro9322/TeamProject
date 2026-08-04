using UnityEngine;

// 불이 언제 누구를 때릴지 계산하는 곳. 아무것도 바꾸지 않고 답만 준다.
public static class BurnTiming
{
    // 줄 맨 앞이 지금 맞을 차례인가. 줄이 비어 있으면 아니다.
    public static bool HasDueUnit()
    {
        return !BurningList.IsEmpty && BurningList.FirstBurning.NextHitTime <= Time.time;
    }

    // 방금 맞은 유닛이 다음에 맞을 시각.
    public static float NextHitTime(float hitInterval)
    {
        return Time.time + hitInterval;
    }

    // 줄에 처음 설 때 적을 시각. 지금으로 두어 올라서자마자 한 대 맞는다.
    public static float FirstHitTime()
    {
        return Time.time;
    }

    // 이 유닛을 줄에 세울 정보 한 줄. 때릴 문은 여기서 한 번만 찾아 둔다.
    public static BurningUnit NewBurning(GameObject unitObject)
    {
        return new BurningUnit
        {
            UnitObject = unitObject,
            DamageTarget = unitObject.GetComponentInParent<IDamageAble>(),
            NextHitTime = FirstHitTime()
        };
    }
}
