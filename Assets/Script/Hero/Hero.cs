using UnityEngine;
using UnityEngine.InputSystem;

public class Hero : MonoBehaviour, IDamageAble
{
    public float Hp => throw new System.NotImplementedException();

    public int Defense => throw new System.NotImplementedException();

    public void Die()
    {
        throw new System.NotImplementedException();
    }

    public void TakeDamage(int damage)
    {
        throw new System.NotImplementedException();
    }

    protected HeroStateMachine stateMachine;
    protected HeroIdleState idleState;
    protected HeroAttackState attackState;
    public HeroIdleState IdleState => idleState;
    public HeroAttackState AttackState => attackState;
    [SerializeField] private Animator anim;
    public Animator Anim => anim;

    protected GameObject target;
    public GameObject Target => target;
    [SerializeField] private float attackSpeed;
    public float AttackSpeed => attackSpeed;
    private void Awake()
    {
        stateMachine = new HeroStateMachine();
        idleState = new HeroIdleState(this, stateMachine);
        attackState = new HeroAttackState(this, stateMachine);

        stateMachine.Initialize(idleState);
    }

    private void Update()
    {
        stateMachine.CurrentState.Update();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Enemy")
        {
            target = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (target == other.gameObject)
        {
            target = null;
        }
    }
}
