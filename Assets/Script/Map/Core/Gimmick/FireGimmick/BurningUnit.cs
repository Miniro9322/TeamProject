using UnityEngine;

// 불타는 유닛 한 명분의 정보. 값만 들고 있고 스스로는 아무 일도 하지 않는다.
public class BurningUnit
{
    public GameObject UnitObject;
    public IDamageAble DamageTarget;
    public float NextHitTime;
}
