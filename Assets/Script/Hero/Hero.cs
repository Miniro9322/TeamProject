using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using VContainer;

public class Hero : MonoBehaviour, IDamageAble, IPlaceAble, IUnit
{
    [Header("유닛 생성 비용")]
    [SerializeField] private int citizenAmount = 2;
    [SerializeField] private List<ProductionType> costType;
    [SerializeField] private List<int> costAmount;
    [SerializeField] private int level = 0;

    public Dictionary<ProductionType, int> Cost
    {
        get
        {
            if (costType.Count != costAmount.Count)
            {
                Debug.LogError("생산 건물에 필요한 자원과 자원량이 매칭되지 않습니다. 다시 설정해주세요");
                return null;
            }
            else
            {
                Dictionary<ProductionType, int> temp = new();

                for (int i = 0; i < costType.Count; i++)
                {
                    temp[costType[i]] = -costAmount[i];
                }

                return temp;
            }
        }
    }

    [Header("유닛 정보")]
    [SerializeField] private List<AttackDataSO> basePattern;
    public List<AttackDataSO> BasePattern => basePattern;

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

    [SerializeField] protected int range = 1;
    [SerializeField] protected RangeShape rangeShape = RangeShape.Diamond;
    public int Range => range;
    [SerializeField] private List<GroundZoneDataSO> auraZones = new();
    private CancellationTokenSource _auraCts;

    private StatContainer sc = new();
    public StatContainer SC => sc;
    public StatContainer Stats => sc;

    public int BlockCount => IsDead ? 0 : (int)SC[StatType.BLK];
    private float currentHp;
    public float Hp => currentHp;
    public int Defense => throw new System.NotImplementedException();
    private bool isDead = false;
    public bool IsDead => isDead;

    [SerializeField] private EnemyAttribute unattackableTarget = EnemyAttribute.Fly | EnemyAttribute.Cloaking;
    //테스트용 코드
    private GameManager gameManager;
    protected BuffManager buffManager;
    public int CitizenAmount => citizenAmount;
    [Inject]
    private void Construct(GameManager gameManager, BuffManager buffManager)
    {
        this.gameManager = gameManager;
        this.buffManager = buffManager;
    }

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

    public void Heal(float amount)
    {
        if (isDead || amount <= 0f)
            return;
        Debug.Log($"before : {currentHp}");
        currentHp = Mathf.Min(currentHp + amount, sc[StatType.HP]);
        Debug.Log($"after : {currentHp}");
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
        OnResur += StartAuras;
    }

    protected virtual void Start()
    {
        SetCurrentTile();
        //테스트용 코드
        if (gameManager != null)
        {
            gameManager.ChangeToDay += Resurrection;
        }
        //끝
        StartAuras();
    }

    //테스트용 코드
    protected virtual void OnDestroy()
    {

        if (gameManager != null)
        {
            gameManager.ChangeToDay -= Resurrection;
        }
        OnResur -= StartAuras;
        _auraCts?.Cancel();
        _auraCts?.Dispose();
    }
    //끝

    // 영웅 주위에 항상 존재하는 오라형 장판을 (재)시작한다. 사망 시 각 장판 루프가 스스로 멈추고,
    // 부활(OnResur)하면 여기가 다시 불려 새 토큰으로 재시작한다.
    private void StartAuras()
    {
        _auraCts?.Cancel();
        _auraCts?.Dispose();
        _auraCts = new CancellationTokenSource();
        foreach (GroundZoneDataSO zone in auraZones)
        {
            if (zone == null) continue;
            // AttackDamageUtil.SpawnGroundZone은 항상 keepAlive=true를 넘겨 임시 장판용이므로,
            // 사망 시 멈춰야 하는 오라는 GroundZoneRunner.Run을 직접 호출해 !IsDead를 넘긴다.
            GroundZoneRunner.Run(transform.position, zone, GetEnemyObjectsInRange, GetAllyObjectsInRange, sc, buffManager,
                () => !IsDead, _auraCts.Token).Forget();
        }
    }
    protected virtual void Update()
    {
        stateMachine.CurrentState.Update();
        if (target != null)
            CheckTargetStillInRange();
        if (target == null)
            AcquireTargetFromTiles();
    }

    protected virtual void AcquireTargetFromTiles()
    {
        GameObject nearest = null;
        float nearestSqrDist = float.MaxValue;

        foreach (Tile tile in TileShapeQuery.GetTiles(board, origin, range, rangeShape))
        {
            foreach (GameObject enemy in tile.Enemies)
            {
                if (!IsTargetable(enemy)) continue;
                float sqrDist = (enemy.transform.position - transform.position).sqrMagnitude;
                if (sqrDist < nearestSqrDist)
                {
                    nearestSqrDist = sqrDist;
                    nearest = enemy;
                }
            }
        }

        if (nearest != null)
        {
            target = nearest;
            context.target = nearest.transform;
        }
    }

    private bool IsTargetable(GameObject enemy)
    {
        if (enemy == null) return false;
        var eb = enemy.GetComponent<EnemyBase>();
        return eb != null && (eb.Attribute & unattackableTarget) == 0;
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

    // GetEnemyObjectsInRange와 동일한 범위 조회지만, 적이 아니라 아군(영웅)을 찾는다.
    // 영웅은 EnemyRegistry 같은 전역 리스트가 없고 이미 타일당 1개 점유자(OccupantObject) 모델을
    // 쓰고 있으므로, 그 점유자를 훑는 방식으로 조회한다(힐/피흡/힐 장판에서 아군 조회용).
    public List<GameObject> GetAllyObjectsInRange(Vector3 originWorld, int range, RangeShape shape = RangeShape.Diamond)
    {
        var found = new List<GameObject>();
        Vector2Int originCell = board.WorldToCell(originWorld);

        foreach (Tile tile in TileShapeQuery.GetTiles(board, originCell, range, shape))
        {
            GameObject occupant = tile.OccupantObject;
            if (occupant == null) continue;
            if (occupant.GetComponent<Hero>() is Hero ally && !ally.IsDead)
                found.Add(occupant);
        }

        return found;
    }

    // 아래 3개는 GetEnemiesInRange/GetEnemyTransformsInRange/GetEnemyObjectsInRange와 동일하되,
    // unattackableTarget 필터를 적용한다. 체인/다수 공격처럼 "특정 적을 타겟으로 선정"하는 로직에서 써서
    // 공격 불가 대상(예: 원거리 전용 대상인 Fly, 저지 전엔 못 때리는 Cloaking)이 뽑히지 않게 한다.
    // 범위(AOE) 스플래시는 의도적으로 이 필터를 타지 않는 기존 메서드를 그대로 쓴다.
    public List<IDamageAble> GetTargetableEnemiesInRange(Vector3 originWorld, int range, RangeShape shape = RangeShape.Diamond)
    {
        var found = new HashSet<IDamageAble>();
        foreach (GameObject enemy in GetTargetableEnemyObjectsInRange(originWorld, range, shape))
        {
            if (enemy.GetComponentInParent<IDamageAble>() is IDamageAble damageable)
                found.Add(damageable);
        }

        return new List<IDamageAble>(found);
    }

    public List<Transform> GetTargetableEnemyTransformsInRange(Vector3 originWorld, int range, RangeShape shape = RangeShape.Diamond)
    {
        var found = new HashSet<Transform>();
        foreach (GameObject enemy in GetTargetableEnemyObjectsInRange(originWorld, range, shape))
            found.Add(enemy.transform);

        return new List<Transform>(found);
    }

    public List<GameObject> GetTargetableEnemyObjectsInRange(Vector3 originWorld, int range, RangeShape shape = RangeShape.Diamond)
    {
        var found = new List<GameObject>();
        foreach (GameObject enemy in GetEnemyObjectsInRange(originWorld, range, shape))
        {
            if (IsTargetable(enemy)) found.Add(enemy);
        }

        return found;
    }

    // originWorld에서 towardWorld 방향으로 4방향 스냅한 직선을 length칸 조회해 적을 모은다.
    public List<IDamageAble> GetEnemiesInLine(Vector3 originWorld, Vector3 towardWorld, int length)
    {
        Vector2Int originCell = board.WorldToCell(originWorld);
        Vector2Int dir = GridCalculator.CardinalToward(originCell, board.WorldToCell(towardWorld));

        var found = new HashSet<IDamageAble>();

        // 근접 저지 구조상 적이 공격자 자신의 칸으로 들어와 저지되므로, origin 칸의 적도 포함한다.
        if (board.TryGetCell(originCell, out Tile originTile))
        {
            foreach (GameObject enemy in originTile.Enemies)
            {
                if (enemy != null && enemy.GetComponentInParent<IDamageAble>() is IDamageAble d)
                    found.Add(d);
            }
        }
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

    protected virtual void CheckTargetStillInRange()
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

    //public async UniTask ResurrectionAfter10s()
    //{
    //    await UniTask.Delay(TimeSpan.FromSeconds(10));
    //    Resurrection();
    //}

    public void SetCurrentTile()
    {
        origin = board.WorldToCell(transform.position);
        if (board.TryGetCell(origin, out Tile current))
            currentTile = current;
    }

    public void ExchangeAttackDatas(List<AttackDataSO> datas, List<AttackSelectorSO> selectors, List<AttackProcSO> procs)
    {
        this.basePattern = datas;
        this.selectors = selectors;
        this.procs = procs;
    }
}