using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class SpiderToxin : EnemyBase
{
    public float hp;
    public float speed;
    public int attack;
    public int def; //테스트용 인스펙터 확인용 스탯들
    private bool isSkill;
    private float skilltimer = 3f;
    
    private void OnEnable()
    {
        isSkill = false;
        IsDie = false;
        Dash().Forget();
    }
    private void Start()
    {
        hp = Hp;
        speed = MoveSpeed;
        attack = AttackPower;
        def = Defense; //테스트용
    }
    private async UniTask Dash()
    {
        float t = 0;
        while(!IsDie)
        {
            if (!isSkill)
            {
                t += Time.deltaTime;
                if (t > skilltimer)
                {
                    Skill().Forget();
                    t =0;
                }
            }
            await UniTask.Yield();
        }
    }

    private async UniTask Skill()
    {
        isSkill = true;
        float t = 0f;
        float duration = 10f;
        Vector3 endpos = new Vector3(transform.position.x+10f,transform.position.y,transform.position.z);
        while(t<1f)
        {
            t += Time.deltaTime*duration;
            transform.position = Vector3.MoveTowards(transform.position,endpos,t);
            await UniTask.Yield();
        }
        isSkill = false;
        transform.position = endpos;
    }
}
