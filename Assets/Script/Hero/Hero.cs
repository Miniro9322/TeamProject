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

    [SerializeField] private StatDataSO statData;

    [SerializeField] private MapBoard board;
    public MapBoard Board => board;
    protected Vector2Int origin;
    protected Tile currentTile;
    public Tile CurrentTile => currentTile;
    //private List<Tile> attackRangedTiles;
    protected int range = 1;
    [SerializeField] protected RangeShape rangeShape = RangeShape.Diamond;

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
    [Inject]
    private void Construct(GameManager gameManager)
    {
        this.gameManager = gameManager;
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
        foreach (Tile tile in TileShapeQuery.GetTiles(board, origin, range, rangeShape))
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

    public List<IDamageAble> GetEnemiesInRange(Vector3 originWorld, int range, RangeShape shape = RangeShape.Diamond)
    {
        var found = new HashSet<IDamageAble>();
        foreach (GameObject enemy in GetEnemyObjectsInRange(originWorld, range, shape))
        {
            if (enemy.GetComponentInParent<IDamageAble>() is IDamageAble damageable)
                found.Add(damageable);
        }

        return new List<IDamageAble>(found);
    }

    public List<Transform> GetEnemyTransformsInRange(Vector3 originWorld, int range, RangeShape shape = RangeShape.Diamond)
    {
        var found = new HashSet<Transform>();
        foreach (GameObject enemy in GetEnemyObjectsInRange(originWorld, range, shape))
            found.Add(enemy.transform);

        return new List<Transform>(found);
    }

    public List<GameObject> GetEnemyObjectsInRange(Vector3 originWorld, int range, RangeShape shape = RangeShape.Diamond)
    {
        var found = new List<GameObject>();
        Vector2Int originCell = board.WorldToCell(originWorld);

        foreach (Tile tile in TileShapeQuery.GetTiles(board, originCell, range, shape))
        {
            foreach (GameObject enemy in tile.Enemies)
            {
                if (enemy == null) continue;
                found.Add(enemy);
            }
        }

        return found;
    }

    // originWorld에서 towardWorld 방향으로 4방향 스냅한 직선을 length칸 조회해 적을 모은다.
    public List<IDamageAble> GetEnemiesInLine(Vector3 originWorld, Vector3 towardWorld, int length)
    {
        Vector2Int originCell = board.WorldToCell(originWorld);
        Vector2Int dir = GridCalculator.CardinalToward(originCell, board.WorldToCell(towardWorld));

        var found = new HashSet<IDamageAble>();
        foreach (Tile tile in TileShapeQuery.GetLineTiles(board, originCell, dir, length))
        {
            foreach (GameObject enemy in tile.Enemies)
            {
                if (enemy != null && enemy.GetComponentInParent<IDamageAble>() is IDamageAble d)
                    found.Add(d);
            }
        }
        return new List<IDamageAble>(found);
    }

    public Vector2Int GetCardinalDirection(Vector3 originWorld, Vector3 towardWorld)
        => GridCalculator.CardinalToward(board.WorldToCell(originWorld), board.WorldToCell(towardWorld));

    public Vector3 GetLineEndPoint(Vector3 originWorld, Vector2Int direction, int length)
    {
        Vector2Int originCell = board.WorldToCell(originWorld);
        List<Tile> line = TileShapeQuery.GetLineTiles(board, originCell, direction, length);
        if (line.Count > 0) return line[line.Count - 1].WorldTop;
        return originWorld + new Vector3(direction.x, 0, direction.y) * length;
    }

    private void CheckTargetStillInRange()
    {
        foreach (Tile tile in TileShapeQuery.GetTiles(board, origin, range, rangeShape))
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
