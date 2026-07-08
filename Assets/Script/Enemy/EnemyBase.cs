using UnityEngine;


public abstract class EnemyBase : IDamageAble
{
    private float hp;
    private int defense;
    private int attackPower;
    private float attackSpeed;

    public float Hp => hp;
    public int Defense => defense;
    public int AttackPower => attackPower;
    public float AttackSpeed => attackSpeed;
    public void TakeDamage(int damage)
    {
        int hitDamage = Mathf.Max(1,damage-defense);
        hp -= hitDamage;
        if(hp<=0)Die();
        
    }

    public virtual void Attack()
    {
        //대충 공격
    }

    public virtual void Die()
    {
        //대충 죽는거
    }
}


