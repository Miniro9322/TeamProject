using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;

public class Hero : MonoBehaviour, IDamageAble, IPlaceAble, IUnit
{
    [Header("유닛 생성 비용")]
    [SerializeField] private int citizenAmount = 2;
    [SerializeField] private List<ResourceCost> cost;
    [SerializeField] private List<BaseUpgradeData> costUpgrades;

    [SerializeField] private List<ResourceCost> statUpgradeCost;
    [SerializeField] private List<BaseUpgradeData> statUpgradeCostUpgrades;
    [SerializeField] private List<BaseUpgradeData> statUpgrades;
    [SerializeField] private List<HeroUpgradeData> upgradeDatas;
    private int skillLevel = 0;
    private int statLevel = 0;
    public int SkillLevel => skillLevel;
    public int StatLevel => statLevel;

    // slot.prefab.GetComponent<Hero>()처럼 Instantiate/Inject를 거치지 않은 프리팹 원본에서
    // Cost를 읽는 경우 upgradeState가 주입돼 있지 않다. UpgradeState는 PlayerPrefs만 읽으면 되는
    // 가벼운 객체라, 주입이 안 된 경우 즉석에서 하나 만들어 최신 해금 상태를 반영한다.
    private UpgradeState UpgradeStateOrFallback => upgradeState ?? new UpgradeState();

    public (ProductionType Type, int Amount)[] Cost
    {
        get
        {
            float discount = UpgradeStateOrFallback.GetTotalEffect(costUpgrades);
            var temp = new (ProductionType, int)[cost.Count];

            for (int i = 0; i < cost.Count; i++)
            {
                temp[i] = (cost[i].Type, -Mathf.RoundToInt(cost[i].Amount * (1f - discount)));
            }

            return temp;
        }
    }

    public (ProductionType Type, int Amount)[] StatUpgradeCost
    {
        get
        {
            float discount = UpgradeStateOrFallback.GetTotalEffect(statUpgradeCostUpgrades);
            var temp = new (ProductionType, int)[statUpgradeCost.Count];

            for (int i = 0; i < statUpgradeCost.Count; i++)
            {
                int baseAmount = statUpgradeCost[i].Amount + statUpgradeCost[i].Amount * statLevel;
                temp[i] = (statUpgradeCost[i].Type, -Mathf.RoundToInt(baseAmount * (1f - discount)));
            }

            return temp;
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
    public StatDataSO StatData => statData;

    // 생성 전 미리보기(정보 패널)용 — 배치된 인스턴스가 아니라 StatContainer가 없으므로,
    // 기본값에 해금된 Hero/Stat 보너스를 직접 더해서 계산한다.
    public float PreviewAttackPower => statData.attackPower + UpgradeStateOrFallback.GetTotalEffect(statUpgrades);
    public float PreviewDefence => statData.defence + UpgradeStateOrFallback.GetTotalEffect(statUpgrades);

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

    // 이펙트 풀은 Hero 인스턴스 소유(Archer/Mage의 projectilePools와 동일한 패턴) —
    // 씬이 언로드돼 이 Hero가 파괴되면 풀도 함께 사라지므로, 파괴된 인스턴스를 다시 꺼내 쓰는 일이 없다.
    private readonly Dictionary<GameObject, IObjectPool<GameObject>> effectPools = new();

    private IObjectPool<GameObject> GetEffectPool(GameObject prefab)
    {
        if (!effectPools.TryGetValue(prefab, out var pool))
        {
            pool = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(prefab),
                actionOnGet: go => { if (go != null) go.SetActive(true); },
                actionOnRelease: go => { if (go != null) go.SetActive(false); },
                actionOnDestroy: go => { if (go != null) Destroy(go); },
                collectionCheck: true,
                defaultCapacity: 8,
                maxSize: 256);
            effectPools[prefab] = pool;
        }
        return pool;
    }

    protected GameObject SpawnEffect(GameObject prefab, Vector3 pos, Quaternion rot, float lifetime)
    {
        GameObject go = SpawnPersistentEffect(prefab, pos, rot);
        if (go != null && lifetime > 0f)
            ReturnEffectAfter(prefab, go, lifetime).Forget();
        return go;
    }

    protected GameObject SpawnPersistentEffect(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (prefab == null) return null;
        IObjectPool<GameObject> pool = GetEffectPool(prefab);
        GameObject go = pool.Get();
        while (go == null) // 다른 경로로 파괴된 채 풀에 있던 인스턴스는 버리고 새로 받는다
            go = pool.Get();
        go.transform.SetPositionAndRotation(pos, rot);
        foreach (ParticleSystem ps in go.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Clear(true);
            ps.Play(true);
        }
        return go;
    }

    protected void DespawnEffect(GameObject prefab, GameObject instance)
    {
        if (prefab == null || instance == null) return;
        GetEffectPool(prefab).Release(instance);
    }

    private async UniTask ReturnEffectAfter(GameObject prefab, GameObject go, float delay)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(delay));
        if (go == null) return; // 대기 중 다른 경로로 이미 파괴됐으면 접근하지 않는다
        DespawnEffect(prefab, go);
    }

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
    private ResourcesManager resourcesManager;
    protected BuffManager buffManager;
    private UpgradeState upgradeState;

    public int CitizenAmount => citizenAmount;
    [Inject]
    private void Construct(GameManager gameManager, BuffManager buffManager, ResourcesManager resourcesManager, UpgradeState upgradeState)
    {
        this.gameManager = gameManager;
        this.buffManager = buffManager;
        this.resourcesManager = resourcesManager;
        this.upgradeState = upgradeState;
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
        currentHp = Mathf.Min(currentHp + amount, sc[StatType.HP]);
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
        ApplyStatUpgradeBonus();
        //테스트용 코드
        if (gameManager != null)
        {
            gameManager.ChangeToDay += Resurrection;
        }
        //끝
        StartAuras();
    }

    private void ApplyStatUpgradeBonus()
    {
        if (upgradeState == null) return;

        float bonus = upgradeState.GetTotalEffect(statUpgrades);
        if (bonus == 0f) return;

        sc.AddModifier(StatType.ATK, new Modifier(ModifierType.Flat, bonus, 0f, StatLayer.Equip, this));
        sc.AddModifier(StatType.DEF, new Modifier(ModifierType.Flat, bonus, 0f, StatLayer.Equip, this));
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
                SpawnEffect, SpawnPersistentEffect, DespawnEffect, () => !IsDead, _auraCts.Token).Forget();
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

    public void SkillUpgrade()
    {
        if (skillLevel >= upgradeDatas.Count)
        {
            return;
        }
        if (resourcesManager.CheckResources(upgradeDatas[skillLevel].Cost))
        {
            resourcesManager.ProductChanged(upgradeDatas[skillLevel].Cost);
            upgradeDatas[skillLevel++].Upgrade(this);
        }
    }
    public void StatUpgrade()
    {
        if (resourcesManager.CheckResources(StatUpgradeCost))
        {
            resourcesManager.ProductChanged(StatUpgradeCost);
            statLevel++;
            Modifier mod = new Modifier(ModifierType.Additive, 0.1f, 0f, StatLayer.Equip, this);
            sc.AddModifier(StatType.HP, mod);
            mod = new Modifier(ModifierType.Additive, 0.05f, 0f, StatLayer.Equip, this);
            sc.AddModifier(StatType.ATK, mod);
            mod = new Modifier(ModifierType.Flat, 1f, 0f, StatLayer.Equip, this);
            sc.AddModifier(StatType.DEF, mod);
        }
    }
}