using Unity.VisualScripting;
using UnityEngine;

public class SpiderNormal : EnemyBase
{
    public float hp =>Hp;
    public float speed =>MoveSpeed;
    public int attack;
    public int def; //테스트용 인스펙터 확인용 스탯들

    protected override void OnEnable()
    {
        base.OnEnable();
        attack = AttackPower;
        def = Defense; //테스트용
    }
}
