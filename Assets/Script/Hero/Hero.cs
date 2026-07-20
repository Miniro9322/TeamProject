using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using VContainer;

public class Hero : MonoBehaviour, IDamageAble, IPlaceAble
{
    [SerializeField] private AttackDataSO[] basePattern;
    public AttackDataSO[] BasePattern => basePattern;

    [SerializeField] private List<AttackSelectorSO> selectors = new();
    public List<AttackSelectorSO> Selectors => selectors;

    [SerializeField] private List<AttackProcSO> procs = new();
    public List<AttackProcSO> Procs => procs;

    public void AddSelector(AttackSelectorSO sel) => selectors.Add(sel);
    public void AddProc(AttackProcSO proc) => procs.Add(proc);

    protected AttackContext context;
    public AttackContext Context => context;

    protected HeroStateMachine stateMachine;
    protected HeroIdleState idleState;
    public HeroIdleState IdleState => idleState;
    protected HeroAttackState attackState;
    public HeroAttackState AttackState => attackState;

    protected HeroDeathState deathState;
    public HeroDeathState DeathState => deathState;

    [SerializeField] private Animator anim;
    [SerializeField] private HeroAnimEvents animEvents;
    public Animator Anim => anim;
    public HeroAnimEvents AnimEvents => animEvents;

    protected GameObject target;
    public GameObject Target => target;

    [SerializeField] private float attackSpeed;
    [SerializeField] private StatDataSO statData;
    public float AttackSpeed => attackSpeed;

    [SerializeField] private MapBoard board;
    public MapBoard Board => board;
    protected Vector2Int origin;
    protected Tile currentTile;
    public Tile CurrentTile => currentTile;
    //private List<Tile> attackRangedTiles;
    protected int range = 1;

    private StatContainer sc = new();
    public StatContainer SC => sc;
    public int BlockCount => IsDead ? 0 : (int)SC[StatType.BLK];
    private float currentHp;
    public float Hp => currentHp;
    public int Defense => throw new System.NotImplementedException();
    private bool isDead = false;
    public bool IsDead => isDead;

    //테스트용 코드
    private GameManager gameManager;
    private CitizenManager citizenManager;
    [Inject]
    private void Construct(GameManager gameManager, CitizenManager citizenManager)
    {
        this.gameManager = gameManager;
        this.citizenManager = citizenManager;
    }
    //끝

    public void Die()
    {
        isDead = true;
        anim.SetBool(HeroAnimHash.idle, false);
        stateMachine.ChangeState(deathState);
        OnBreak?.Invoke();
        // ResurrectionAfter10s().Forget();
    }
    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        currentHp -= damage;
        if (currentHp <= 0)
            Die();
    }
    protected OccupantKind occupantKind;

    public event Action OnBreak;
    public event Action OnResur;

    protected virtual void Awake()
    {
        stateMachine = new HeroStateMachine();
        idleState = new HeroIdleState(this, stateMachine);
        deathState = new HeroDeathState(this, stateMachine);
        stateMachine.Initialize(idleState);
        //attackRangedTiles = board.GetTiles(origin, range);
        sc.AddStat(StatType.HP, statData.maxHp);
        sc.AddStat(StatType.ATK, statData.attackPower);
        sc.AddStat(StatType.DEF, statData.defence);
        sc.AddStat(StatType.BLK, statData.blockCount);
        sc.AddStat(StatType.AS, statData.attackSpeed);
        currentHp = sc[StatType.HP];
        //테스트용 코드
        citizenManager.UseCitizen(2);
        //끝
    }

    protected virtual void Start()
    {
        origin = board.WorldToCell(transform.position);
        if (board.TryGetCell(origin, out Tile current))
            currentTile = current;
        currentTile.SetOccupant(this.gameObject, occupantKind);
        //테스트용 코드
        if(gameManager != null)
        {
            gameManager.ChangeToDay += Resurrection;
        }
        //끝
    }

    //테스트용 코드
    protected virtual void OnDestroy()
    {
        
        if (gameManager != null)
        {
            gameManager.ChangeToDay -= Resurrection;
        }
    }
    //끝
    protected virtual void Update()
    {
        stateMachine.CurrentState.Update();
        if (target != null)
            CheckTargetStillInRange();
        if (target == null)
            AcquireTargetFromTiles();
    }

    private void AcquireTargetFromTiles()
    {
        foreach (Tile tile in board.GetTiles(origin, range, false))
        {
            foreach (GameObject enemy in tile.Enemies)
            {
                if (enemy == null) continue;
                target = enemy;
                context.target = target.transform;
                return;
            }
        }
    }

    public List<IDamageAble> GetEnemiesInRange(Vector3 originWorld, int range, bool square = false)
    {
        var found = new HashSet<IDamageAble>();
        Vector2Int originCell = board.WorldToCell(originWorld);

        foreach (Tile tile in board.GetTiles(originCell, range, square))
        {
            foreach (GameObject enemy in tile.Enemies)
            {
                if (enemy == null) continue;
                if (enemy.GetComponentInParent<IDamageAble>() is IDamageAble damageable)
                    found.Add(damageable);
            }
        }

        return new List<IDamageAble>(found);
    }

    private void CheckTargetStillInRange()
    {
        foreach (Tile tile in board.GetTiles(origin, range, false))
        {
            foreach (GameObject enemy in tile.Enemies)
            {
                if (enemy == target)
                    return;
            }
        }
        target = null;
        context.target = null;
    }

    public void SetBoard(MapBoard board)
    {
        this.board = board;
    }

    public void Resurrection()
    {
        currentHp = sc[StatType.HP];
        isDead = false;
        OnResur?.Invoke();
    }

    public async UniTask ResurrectionAfter10s()
    {
        await UniTask.Delay(TimeSpan.FromSeconds(10));
        Resurrection();
    }
}
