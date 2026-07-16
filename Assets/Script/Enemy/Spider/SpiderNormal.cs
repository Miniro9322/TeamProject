using Unity.VisualScripting;
using UnityEngine;

public class SpiderNormal : EnemyBase
{
    public float hp;
    public float speed;
    public int attack;
    public int def; //테스트용 인스펙터 확인용 스탯들

    protected override void OnEnable()
    {
        base.OnEnable();
        hp = Hp;
        speed = MoveSpeed;
        attack = AttackPower;
        def = Defense; //테스트용
    }
}
