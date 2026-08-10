using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;

public enum RangeQueryAffinity { Enemy, TargetableEnemy, Ally }

public class Hero : MonoBehaviour, IDamageAble, IUnit, IStunAble, IDebuffCarrier
{
    [SerializeField] private List<BaseUpgradeData> statUpgrades;
    [SerializeField] private HeroData heroData;
    public int Tier => heroData.Tier;
    public int UnitId => heroData.UnitId;
    public string HeroName => heroData.HeroName;
    public MergeKey MergeKey => heroData.MergeKey;

    // 티어 단위로 공유되는 업그레이드 레벨(개체별로 갖지 않음) — HeroTierUpgradeState 참고.
    public int Level => tierUpgradeState.GetLevel(Tier);

    private UpgradeState UpgradeStateOrFallback => upgradeState ?? new UpgradeState();

    [Header("유닛 정보")]
    [SerializeField] private List<AttackDataSO> basePattern;
    public List<AttackDataSO> BasePattern => basePattern;
    // 히어로의 "현재 공격 데이터" — 항상 basePattern[0] 고정. 로테이션 중인 실제 공격과는 무관하게
    // 배치 프리뷰(RangeInfo)나 사거리 판정처럼 전투 시작 전에도 값이 있어야 하고 전투 중에도 흔들리면
    // 안 되는 곳에서 쓴다.
    public AttackDataSO CurrentAttackData => basePattern != null && basePattern.Count > 0 ? basePattern[0] : null;

    // 트레잇/액티브 스킬은 이제 SO가 아니라 같은 프리팹에 붙은 Component다 — Awake에서 자동 수집한다.
    private HeroTrait[] traits;
    public IReadOnlyList<HeroTrait> Traits => traits;

    private HeroActiveSkill activeSkill;
    public HeroActiveSkill ActiveSkill => activeSkill;
    private float skillCooldownRemaining;
    public bool IsSkillReady => activeSkill == null || skillCooldownRemaining <= 0f;
    private CancellationTokenSource skillCts;

    protected AttackContext context;
    public AttackContext Context => context;

    protected HeroStateMachine stateMachine;
    protected HeroIdleState idleState;
    public HeroIdleState IdleState => idleState;
    protected HeroAttackState attackState;
    public HeroAttackState AttackState => attackState;
    protected HeroDeathState deathState;
    public HeroDeathState DeathState => deathState;
    protected HeroStunState stunState;
    public HeroStunState StunState => stunState;

    [SerializeField] private Animator anim;
    [SerializeField] private HeroAnimEvents animEvents;
    public Animator Anim => anim;
    public HeroAnimEvents AnimEvents => animEvents;

    private HeroOutlineEffect outlineEffect;
    public void SetSelected(bool selected)
    {
        outlineEffect ??= new HeroOutlineEffect(transform);
        outlineEffect.SetActive(selected);
    }

    protected GameObject target;
    public GameObject Target => target;

    [SerializeField] private StatDataSO statData;
    public StatDataSO StatData => statData;
    public float PreviewAttackPower => statData.attackPower + UpgradeStateOrFallback.GetTotalEffect(statUpgrades);
    public float PreviewDefence => statData.defence + UpgradeStateOrFallback.GetTotalEffect(statUpgrades);

    [SerializeField] private MapBoard board;
    public MapBoard Board => board;
    protected Vector2Int origin;
    protected Tile currentTile;
    public Tile CurrentTile => currentTile;

    // 사거리/사거리 형태는 AttackDataSO(CurrentAttackData = basePattern[0])로 이전됨 — 프로퍼티
    // 이름/시그니처는 그대로 유지해 RangeInfo 등 기존 소비 코드는 변경 없이 그대로 쓴다.
    public int Range => CurrentAttackData?.range ?? 0;
    public RangeShape RangeShape => CurrentAttackData?.rangeShape ?? RangeShape.Diamond;

    [Tooltip("소유자가 죽을 때까지 유지되는 오라 장판(GroundZoneEffect, duration<=0) 프리팹들")]
    [SerializeField] private List<GameObject> auraZonePrefabs = new();

    // 이펙트 풀은 Hero 인스턴스 소유(Archer/Mage의 projectilePools와 동일 패턴) — 씬이 언로드돼 이 Hero가
    // 파괴되면 풀도 함께 사라지므로, 파괴된 인스턴스를 다시 꺼내 쓰는 일이 없다.
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

    // 투사체 풀도 이펙트 풀과 동일한 패턴(Hero 인스턴스 소유) — 원거리 Hero(Archer/Mage)만 사용한다.
    private readonly Dictionary<Projectile, IObjectPool<Projectile>> projectilePools = new();

    public IObjectPool<Projectile> GetProjectilePool(Projectile prefab)
    {
        if (!projectilePools.TryGetValue(prefab, out var pool))
        {
            pool = new ObjectPool<Projectile>(
                createFunc: () => Instantiate(prefab),
                actionOnGet: p => p.gameObject.SetActive(true),
                actionOnRelease: p => p.gameObject.SetActive(false),
                actionOnDestroy: p => Destroy(p.gameObject),
                collectionCheck: true,
                defaultCapacity: 8,
                maxSize: 32);
            projectilePools[prefab] = pool;
        }
        return pool;
    }

    // HeroTrait/GroundZoneEffect(같은 GameObject의 다른 컴포넌트)도 써야 해서 public.
    public GameObject SpawnEffect(GameObject prefab, Vector3 pos, Quaternion rot, float lifetime)
    {
        GameObject go = SpawnPersistentEffect(prefab, pos, rot);
        if (go != null && lifetime > 0f)
            ReturnEffectAfter(prefab, go, lifetime).Forget();
        return go;
    }

    public GameObject SpawnPersistentEffect(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (prefab == null) return null;
        IObjectPool<GameObject> pool = GetEffectPool(prefab);
        GameObject go = pool.Get();
        while (go == null)
            go = pool.Get();
        go.transform.SetPositionAndRotation(pos, rot);
        foreach (ParticleSystem ps in go.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Clear(true);
            ps.Play(true);
        }
        return go;
    }

    public void DespawnEffect(GameObject prefab, GameObject instance)
    {
        if (prefab == null || instance == null) return;
        GetEffectPool(prefab).Release(instance);
    }

    // 체인 튕김 두 지점(from/to)을 잇는 이펙트. SpawnEffect의 풀링/회수 타이머를 그대로 감싸고
    // 두 점만 추가로 세팅한다.
    public GameObject SpawnChainArc(GameObject prefab, Vector3 from, Vector3 to, float lifetime)
    {
        GameObject go = SpawnEffect(prefab, from, Quaternion.identity, lifetime);
        UpdateLinkEndpoints(go, from, to);
        return go;
    }

    // 빔/체인 두 점 이펙트의 끝점을 갱신하는 공용 헬퍼. BeamLinkEffect가 붙어 있으면 텍스처 스크롤/히트
    // 이펙트 배치까지 맡기고, 없으면(단순 LineRenderer만 있는 프리팹) 두 점만 직접 세팅하는 폴백을
    // 유지한다 — ContinuousBeamStrategy의 매 tick 갱신에서도 재사용.
    public static void UpdateLinkEndpoints(GameObject go, Vector3 from, Vector3 to)
    {
        if (go == null) return;
        if (go.TryGetComponent(out BeamLinkEffect link))
            link.SetEndpoints(from, to);
        else if (go.TryGetComponent(out LineRenderer lr))
        {
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
        }
    }

    private async UniTask ReturnEffectAfter(GameObject prefab, GameObject go, float delay)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(delay));
        if (go == null) return;
        DespawnEffect(prefab, go);
    }

    // GroundZoneEffect 프리팹을 풀에서 꺼내 위치를 잡고 Init만 넘긴다 — 이후 틱/소멸(풀 반납)은
    // GroundZoneEffect 컴포넌트가 스스로 처리한다.
    public void SpawnGroundZone(GameObject prefab, Vector3 pos)
    {
        if (prefab == null) return;
        IObjectPool<GameObject> pool = GetEffectPool(prefab);
        GameObject go = pool.Get();
        while (go == null)
            go = pool.Get();
        go.transform.position = pos;
        if (go.TryGetComponent(out GroundZoneEffect zone))
            zone.Init(Board, this, released => pool.Release(released));
    }

    private StatContainer sc = new();
    public StatContainer SC => sc;
    public StatContainer Stats => sc;

    public int BlockCount => IsDead ? 0 : (int)SC[StatType.BLK];
    private float currentHp;
    public float Hp => currentHp;
    public int Defense => Mathf.RoundToInt(sc[StatType.DEF]); // 기존 NotImplementedException 버그 수정
    private bool isDead;
    public bool IsDead => isDead;

    private readonly DebuffTracker debuffTracker = new();
    public DebuffTracker Debuffs => debuffTracker;
    public DebuffType ImmuneDebuffs => DebuffType.None;

    public bool IsStunned => debuffTracker.Has(DebuffType.Stun);

    public void Stun(float duration)
    {
        if (isDead || duration <= 0f) return;
        bool wasStunned = IsStunned;
        debuffTracker.Apply(DebuffType.Stun, duration);
        if (!wasStunned) stateMachine.ChangeState(stunState);
    }

    private readonly EnemyDebuffEffects debuffEffects = new();


    private GameManager gameManager;
    private ResourcesManager resourcesManager;
    protected BuffManager buffManager;
    public BuffManager Buffs => buffManager;
    private UpgradeState upgradeState;
    private HeroTierUpgradeState tierUpgradeState;

    [Inject]
    private void Construct(GameManager gameManager, BuffManager buffManager, ResourcesManager resourcesManager, UpgradeState upgradeState, HeroTierUpgradeState tierUpgradeState)
    {
        this.gameManager = gameManager;
        this.buffManager = buffManager;
        this.resourcesManager = resourcesManager;
        this.upgradeState = upgradeState;
        this.tierUpgradeState = tierUpgradeState;
    }

    public void Die()
    {
        isDead = true;
        anim.SetBool(HeroAnimHash.idle, false);
        stateMachine.ChangeState(deathState);
        debuffEffects.Reset();
        skillCts?.Cancel();
        skillCts?.Dispose();
        skillCts = null;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        int hitDamage = Mathf.Max(1, damage - Defense);
        currentHp -= hitDamage;
        if (currentHp <= 0) Die();
    }

    public void Heal(float amount)
    {
        if (isDead || amount <= 0f) return;
        currentHp = Mathf.Min(currentHp + amount, sc[StatType.HP]);
    }

    [SerializeField] protected OccupantKind occupantKind;
    public OccupantKind OccupantKind => occupantKind;

    protected virtual void Awake()
    {
        stateMachine = new HeroStateMachine();
        idleState = new HeroIdleState(this, stateMachine);
        deathState = new HeroDeathState(this, stateMachine);
        stunState = new HeroStunState(this, stateMachine);
        stateMachine.Initialize(idleState);

        sc.AddStat(StatType.HP, statData.maxHp);
        sc.AddStat(StatType.ATK, statData.attackPower);
        sc.AddStat(StatType.DEF, statData.defence);
        sc.AddStat(StatType.BLK, statData.blockCount);
        sc.AddStat(StatType.AS, statData.attackSpeed);
        currentHp = sc[StatType.HP];

        traits = GetComponents<HeroTrait>();
        activeSkill = GetComponent<HeroActiveSkill>();

        GameObject stunEffectPrefab = Resources.Load<GameObject>("EnemyEffectPrefab/Stun");
        var debuffEffectSet = Resources.Load<DebuffEffectSetSO>("EnemyEffectPrefab/DebuffEffectSet");
        debuffEffects.Setup(debuffEffectSet, transform, transform, transform, stunEffectPrefab);

        EnsureClickCollider();
    }

    // 프리팹에 콜라이더가 없어(순수 렌더러만 있음) 맵의 타일 판정만으로는 캐릭터 모델을 직접 클릭해
    // 선택할 수 없다. 렌더러 전체를 감싸는 콜라이더를 하나 붙여 PointerPick이 물리 레이캐스트로
    // "영웅 몸통을 직접 클릭"한 경우를 잡아낼 수 있게 한다.
    private void EnsureClickCollider()
    {
        if (TryGetComponent<Collider>(out _)) return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        Bounds worldBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) worldBounds.Encapsulate(renderers[i].bounds);

        BoxCollider collider = gameObject.AddComponent<BoxCollider>();
        collider.center = transform.InverseTransformPoint(worldBounds.center);
        collider.size = worldBounds.size;
    }

    protected virtual void Start()
    {
        SetCurrentTile();
        ApplyStatUpgradeBonus();
        ApplyTierLevelBonus();
        tierUpgradeState.LevelChanged += OnTierLevelChanged;
        if (gameManager != null)
        {
            gameManager.ChangeToDay += Resurrection;
            gameManager.ChangeToDay += ResetSkillCooldown;
        }
        SpawnAuraZones();
    }

    // GroundZoneEffect(duration<=0)는 소유자가 죽으면 스스로 감지하고 풀에 반납되므로, 부활 시엔
    // 그냥 다시 스폰하면 된다 — 예전의 _auraCts 취소/재시작 관리가 필요 없어졌다.
    private void SpawnAuraZones()
    {
        foreach (GameObject prefab in auraZonePrefabs)
            SpawnGroundZone(prefab, transform.position);
    }

    private void ApplyStatUpgradeBonus()
    {
        if (upgradeState == null) return;

        float bonus = upgradeState.GetTotalEffect(statUpgrades);
        if (bonus == 0f) return;

        sc.AddModifier(StatType.ATK, new Modifier(ModifierType.Flat, bonus, 0f, StatLayer.Equip, this));
        sc.AddModifier(StatType.DEF, new Modifier(ModifierType.Flat, bonus, 0f, StatLayer.Equip, this));
    }

    private static readonly object TierLevelBonusSource = new object();

    private void ApplyTierLevelBonus()
    {
        sc.RemoveModifier(TierLevelBonusSource);
        int extraLevels = tierUpgradeState.GetLevel(Tier);
        if (extraLevels <= 0) return;

        foreach (var gain in tierUpgradeState.GetStatGains(Tier))
            sc.AddModifier(gain.statType, new Modifier(gain.modifierType, gain.amountPerLevel * extraLevels, 0f, StatLayer.Equip, TierLevelBonusSource));
    }

    private void OnTierLevelChanged(int changedTier)
    {
        if (changedTier == Tier) ApplyTierLevelBonus();
    }

    protected virtual void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.ChangeToDay -= Resurrection;
            gameManager.ChangeToDay -= ResetSkillCooldown;
        }
        tierUpgradeState.LevelChanged -= OnTierLevelChanged;
        debuffEffects.Reset();
        skillCts?.Cancel();
        skillCts?.Dispose();
        skillCts = null;
    }

    // ---- 트레잇 훅 팬아웃 — HeroAttackRunner/GroundZoneEffect/AttackDamageUtil이 호출한다 ----
    public void NotifyAttackPerformed(AttackDataSO data)
    {
        for (int i = 0; i < traits.Length; i++) traits[i]?.OnAttackPerformed(data);
    }

    public void NotifyHit(GameObject hitTarget, int amount, bool isCrit)
    {
        for (int i = 0; i < traits.Length; i++) traits[i]?.OnHit(hitTarget, amount, isCrit);
        if (hitTarget != null && hitTarget.GetComponentInParent<IDamageAble>() is IDamageAble d && d.Hp <= 0f)
            NotifyKill(hitTarget);
    }

    public void NotifyKill(GameObject killedTarget)
    {
        for (int i = 0; i < traits.Length; i++) traits[i]?.OnKill(killedTarget);
    }

    // ---- 다음 공격 오버라이드 (트레잇의 N타 강공 / 확률 재발동) ----
    private AttackDataSO queuedOverride;
    private bool queuedOverrideIsProc;
    public AttackDataSO LastUsedAttackData { get; private set; }
    public bool LastAttackWasProc { get; private set; }

    public void QueueNextAttackOverride(AttackDataSO data, bool isProc = false)
    {
        queuedOverride = data;
        queuedOverrideIsProc = isProc;
    }

    public AttackDataSO ConsumeAttackOverride(out bool isProc)
    {
        isProc = queuedOverrideIsProc;
        AttackDataSO data = queuedOverride;
        queuedOverride = null;
        queuedOverrideIsProc = false;
        return data;
    }

    public void SetLastUsedAttack(AttackDataSO data, bool isProc)
    {
        LastUsedAttackData = data;
        LastAttackWasProc = isProc;
    }

    // AttackStanceSkill처럼 지속시간 동안 basePattern[0]을 바꿔치기하는 스킬용.
    public AttackDataSO SwapPrimaryAttackData(AttackDataSO next)
    {
        if (basePattern.Count == 0) return null;
        AttackDataSO previous = basePattern[0];
        basePattern[0] = next != null ? next : previous;
        return previous;
    }

    // ---- 액티브 스킬 (HeroSkillCastController가 플레이어 클릭을 받아 호출) ----
    public bool TryUseActiveSkill(Tile targetTile)
    {
        if (activeSkill == null || isDead || targetTile == null || targetTile.Board != Board)
            return false;
        if (!IsSkillReady)
            return false;

        if (activeSkill.targetScope == SkillTargetScope.Self)
        {
            Vector2Int casterCell = Board.WorldToCell(transform.position);
            if (targetTile.Coord != casterCell) return false;
        }
        // AnywhereOnBoard: 위에서 이미 targetTile.Board == Board를 확인했으므로 거리 제한 없이 통과.

        skillCooldownRemaining = activeSkill.cooldown;

        if (activeSkill.blocksBasicAttack)
            stateMachine.ChangeState(new HeroSkillState(this, stateMachine, targetTile));
        else
            RunActiveSkillUnblocked(targetTile).Forget();

        return true;
    }

    private async UniTask RunActiveSkillUnblocked(Tile targetTile)
    {
        skillCts?.Cancel();
        skillCts?.Dispose();
        skillCts = new CancellationTokenSource();
        try { await activeSkill.Execute(targetTile, skillCts.Token); }
        catch (OperationCanceledException) { }
    }

    private void ResetSkillCooldown() => skillCooldownRemaining = 0f;

    protected virtual void Update()
    {
        if (target != null)
            CheckTargetStillInRange();
        if (target == null)
            AcquireTargetFromTiles();

        stateMachine.CurrentState.Update();
        if (skillCooldownRemaining > 0f) skillCooldownRemaining -= Time.deltaTime;

        for (int i = 0; i < traits.Length; i++)
            traits[i]?.OnPassiveTick(Time.deltaTime);

        debuffEffects.Tick(debuffTracker, isDead);
    }

    protected virtual void AcquireTargetFromTiles()
    {
        GameObject nearest = null;
        float nearestSqrDist = float.MaxValue;

        foreach (Tile tile in TileShapeQuery.GetTiles(Board, origin, Range, RangeShape))
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
        return eb != null && !eb.IsDead && (eb.Attribute & (CurrentAttackData?.unattackableTarget ?? EnemyAttribute.None)) == 0;
    }

    // Enemy = 필터 없음(AOE 스플래시용 — 의도적으로 unattackableTarget을 타지 않음).
    // TargetableEnemy = unattackableTarget 필터 적용(체인/멀티샷처럼 "특정 적을 타겟으로 선정"할 때).
    // Ally = 타일 점유자(OccupantObject) 기준 아군 조회. 영웅은 EnemyRegistry 같은 전역 리스트가
    // 없고 이미 타일당 1개 점유자 모델을 쓰고 있으므로 그 점유자를 훑는다(힐/피흡/힐 장판/오라용).
    // List + "이미 본 것" HashSet을 함께 써서 중복은 제거하되 타일 순회 순서는 유지한다 —
    // AttackTargetSelector.SelectTargets가 결과 리스트의 순서(pool[i % poolSize])에 의존하므로
    // HashSet 하나로만 중복 제거하면(순서 미보장) 멀티샷 대상 선정이 매 프레임 흔들릴 수 있다.
    public List<GameObject> GetObjectsInRange(Vector3 originWorld, int range, RangeShape shape, RangeQueryAffinity affinity = RangeQueryAffinity.Enemy)
    {
        Vector2Int originCell = Board.WorldToCell(originWorld);
        var found = new List<GameObject>();
        var seen = new HashSet<GameObject>();

        if (affinity == RangeQueryAffinity.Ally)
        {
            foreach (Tile tile in TileShapeQuery.GetTiles(Board, originCell, range, shape))
            {
                GameObject occupant = tile.OccupantObject;
                if (occupant != null && occupant.GetComponent<Hero>() is Hero ally && !ally.IsDead && seen.Add(occupant))
                    found.Add(occupant);
            }
            return found;
        }

        foreach (Tile tile in TileShapeQuery.GetTiles(Board, originCell, range, shape))
            foreach (GameObject enemy in tile.Enemies)
                if (enemy != null && (affinity == RangeQueryAffinity.Enemy || IsTargetable(enemy)) && seen.Add(enemy))
                    found.Add(enemy);

        return found;
    }

    public List<IDamageAble> GetEnemiesInLine(Vector3 originWorld, Vector3 towardWorld, int length, int width = 0)
    {
        Vector2Int originCell = Board.WorldToCell(originWorld);
        Vector2Int dir = GridCalculator.CardinalToward(originCell, Board.WorldToCell(towardWorld));

        var found = new HashSet<IDamageAble>();

        if (Board.TryGetCell(originCell, out Tile originTile))
            foreach (GameObject enemy in originTile.Enemies)
                if (enemy != null && enemy.GetComponentInParent<IDamageAble>() is IDamageAble d)
                    found.Add(d);

        foreach (Tile tile in TileShapeQuery.GetLineTiles(Board, originCell, dir, length, width))
            foreach (GameObject enemy in tile.Enemies)
                if (enemy != null && enemy.GetComponentInParent<IDamageAble>() is IDamageAble d)
                    found.Add(d);

        return new List<IDamageAble>(found);
    }

    public Vector2Int GetCardinalDirection(Vector3 originWorld, Vector3 towardWorld)
        => GridCalculator.CardinalToward(Board.WorldToCell(originWorld), Board.WorldToCell(towardWorld));

    public Vector3 GetLineEndPoint(Vector3 originWorld, Vector2Int direction, int length)
    {
        Vector2Int originCell = Board.WorldToCell(originWorld);
        List<Tile> line = TileShapeQuery.GetLineTiles(Board, originCell, direction, length);
        if (line.Count > 0) return line[line.Count - 1].WorldTop;
        return originWorld + new Vector3(direction.x, 0, direction.y) * length;
    }

    protected virtual void CheckTargetStillInRange()
    {
        if (!IsTargetable(target))
        {
            target = null;
            context.target = null;
            return;
        }

        foreach (Tile tile in TileShapeQuery.GetTiles(Board, origin, Range, RangeShape))
            foreach (GameObject enemy in tile.Enemies)
                if (enemy == target)
                    return;

        target = null;
        context.target = null;
    }

    public void SetBoard(MapBoard board) => this.board = board;

    public void Resurrection()
    {
        currentHp = sc[StatType.HP];
        isDead = false;
        SpawnAuraZones();
    }

    public void SetCurrentTile()
    {
        origin = Board.WorldToCell(transform.position);
        if (Board.TryGetCell(origin, out Tile current))
            currentTile = current;
    }

    public void ExchangeAttackDatas(List<AttackDataSO> datas) => basePattern = datas;

}
