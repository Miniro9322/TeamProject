using UnityEngine;


public abstract class EnemyBase : MonoBehaviour,IDamageAble
{
    [SerializeField] protected string enemyKey;
    public float Hp { get; protected set; }
    public int Defense { get; protected set; }
    public int AttackPower { get; protected set; }
    public float AttackSpeed { get; protected set; }
    public int Range { get; protected set; }
    public float MoveSpeed { get; protected set; }
    public bool IsDie { get; protected set; }
    public Animator animator;

    protected virtual void Awake()
    {
        LoadStats();
    }

    protected void LoadStats()
    {
        if (string.IsNullOrEmpty(enemyKey)) enemyKey = GetType().Name;

        var enemyTable = DataTableManager.Get<EnemyTable>(DataTableIds.Enemy);
        if (enemyTable == null) return;

        var data = enemyTable.Get(enemyKey);
        if (data == null)
        {
            Debug.LogWarning($"EnemyBase: EnemyTable에서 '{enemyKey}' 데이터를 찾을 수 없음");
            return;
        }
        ApplyData(data);
    }

    protected virtual void ApplyData(EnemyTable.Data data)
    {
        AttackPower = data.Attack;
        AttackSpeed = data.AttackSpeed;
        Range = data.Range;
        Defense = data.Defense;
        Hp = data.Health;
        MoveSpeed = data.MoveSpeed;
        IsDie = false;
    }

    public void TakeDamage(int damage)
    {
        if(IsDie)return;
        int hitDamage = Mathf.Max(1,damage-Defense);
        Hp -= hitDamage;
        if(Hp<=0)Die();

    }

    public virtual void Attack()
    {
        //대충 공격
    }

    public virtual void Die()
    {
        if(IsDie)return;
        IsDie= true;
        //대충 죽는거
    }
}


