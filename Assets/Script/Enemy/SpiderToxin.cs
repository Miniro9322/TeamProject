using System.Collections;
using UnityEngine;

public class SpiderToxin : EnemyBase
{
    public float hp;
    public float speed;
    public int attack;
    public int def; //테스트용 인스펙터 확인용 스탯들
    private bool isSkill;
    private float skilltimer = 3f;
    
    private void OEnable()
    {
        isSkill = false;
        skilltimer = 0;
    }
    private void Start()
    {
        hp = Hp;
        speed = MoveSpeed;
        attack = AttackPower;
        def = Defense; //테스트용
    }
    private IEnumerator Dash()
    {
        float t = 0;
        while(!IsDie)
        {
            if (!isSkill)
            {
                t += Time.deltaTime;
                if (t > skilltimer)
                {
                    // Skill();
                    t =0;
                }
            }
            yield return null;
        }
    }
}
