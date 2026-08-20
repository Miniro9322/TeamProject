using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;
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
    public int HeroType => heroData.HeroType;

    // 티어 단위로 공유되는 업그레이드 레벨(개체별로 갖지 않음) — HeroTierUpgradeState 참고.
    public int Level => tierUpgradeState.GetLevel(Tier) + 1;
    // 클래스(근거리/원거리) 단위로 공유되는 업그레이드 레벨 — HeroClassUpgradeState 참고.
    public int ClassLevel => classUpgradeState.GetLevel(HeroType) + 1;

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
        if (instance.TryGetComponent(out BeamLinkEffect link)) link.StopTracking();
        GetEffectPool(prefab).Release(instance);
    }

    // 체인 튕김 두 지점을 잇는 이펙트. 좌표가 아니라 대상 GameObject를 받아서, 스폰 후에도
    // TrackLinkEndpoints로 계속 그 대상들을 따라가게 한다(대상이 움직이는 동안 얼어붙지 않도록).
    public GameObject SpawnChainArc(GameObject prefab, GameObject fromTarget, GameObject toTarget, float lifetime)
    {
        GameObject go = SpawnEffect(prefab, AttackDamageUtil.EffectPosition(fromTarget), Quaternion.identity, lifetime);
        TrackLinkEndpoints(go, AttackDamageUtil.TrackingPosition(fromTarget), AttackDamageUtil.TrackingPosition(toTarget));
        return go;
    }

    // 빔/체인 두 점 이펙트의 끝점을 한 번만 적용하는 공용 헬퍼. BeamLinkEffect가 붙어 있으면 텍스처
    // 스크롤/히트 이펙트 배치까지 맡기고, 없으면(단순 LineRenderer만 있는 프리팹) 두 점만 직접
    // 세팅하는 폴백을 유지한다.
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

    // 매 프레임 스스로 갱신하도록 두 지점 제공자를 등록한다. BeamLinkEffect가 있으면 Track으로
    // 넘겨 매 프레임 다시 계산하게 하고, 없으면(단순 LineRenderer 프리팹) 기존처럼 1회만 적용한다.
    public static void TrackLinkEndpoints(GameObject go, Func<Vector3> from, Func<Vector3> to)
    {
        if (go == null) return;
        if (go.TryGetComponent(out BeamLinkEffect link))
            link.Track(from, to);
        else
            UpdateLinkEndpoints(go, from(), to());
    }

    private async UniTask ReturnEffectAfter(GameObject prefab, GameObject go, float delay)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(delay));
        if (go == null) return;
        DespawnEffect(prefab, go);
    }

    // GroundZoneEffect 프리팹을 풀에서 꺼내 위치를 잡고 Init만 넘긴다 — 이후 틱/소멸(풀 반납)은
    // GroundZoneEffect 컴포넌트가 스스로 처리한다.
    // TilePainter.lift(0.02f)와 동일한 값 — 바닥 메시 윗면과 같은 높이에 놓이면 알파블렌드 장판
    // 링 VFX가 오파크 바닥과 z-fighting을 일으켜 깜빡여 보인다.
    private const float GroundZoneLift = 0.02f;

    public void SpawnGroundZone(GameObject prefab, Vector3 pos, bool followOwner = false)
    {
        if (prefab == null) return;
        IObjectPool<GameObject> pool = GetEffectPool(prefab);
        GameObject go = pool.Get();
        while (go == null)
            go = pool.Get();
        go.transform.SetParent(followOwner ? transform : null, worldPositionStays: false);
        go.transform.position = pos + Vector3.up * GroundZoneLift;
        if (go.TryGetComponent(out GroundZoneEffect zone))
            zone.Init(Board, this, followOwner, released => pool.Release(released));
    }

    private StatContainer sc = new();
    public StatContainer SC => sc;
    public StatContainer Stats => sc;

    public int BlockCount => IsDead ? 0 : (int)SC[StatType.BLK];
    private float currentHp;
    public float Hp => currentHp;
    public float MaxHp => sc[StatType.HP];
    public int Defense => Mathf.RoundToInt(sc[StatType.DEF]); // 기존 NotImplementedException 버그 수정
    private bool isDead;
    public bool IsDead => isDead;

    [SerializeField] private Slider healthSlider;
    private readonly HeroHealthBar _bar = new();

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
    private HeroClassUpgradeState classUpgradeState;

    [Inject]
    private void Construct(GameManager gameManager, BuffManager buffManager, ResourcesManager resourcesManager, UpgradeState upgradeState, HeroTierUpgradeState tierUpgradeState, HeroClassUpgradeState classUpgradeState)
    {
        this.gameManager = gameManager;
        this.buffManager = buffManager;
        this.resourcesManager = resourcesManager;
        this.upgradeState = upgradeState;
        this.tierUpgradeState = tierUpgradeState;
        this.classUpgradeState = classUpgradeState;
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

    public void TakeDamage(int damage,bool ignore = false)
    {
        if (isDead) return;
        
        int hitDamage = Mathf.Max(1, damage - (ignore ? 0 : Defense));
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
        _bar.Setup(healthSlider, 10f);
        _bar.ResetTo(currentHp, sc[StatType.HP]);

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
        ApplyClassLevelBonus();
        currentHp = sc[StatType.HP]; // 티어/클래스 업그레이드로 늘어난 최대체력을 스폰 시점부터 반영
        tierUpgradeState.LevelChanged += OnTierLevelChanged;
        classUpgradeState.LevelChanged += OnClassLevelChanged;
        if (gameManager != null)
        {
            gameManager.ChangeToDay += Resurrection;
            gameManager.ChangeToDay += HealFull;
            gameManager.ChangeToDay += ResetSkillCooldown;
            gameManager.ChangeToDay += NotifyDayStart;
        }
        SpawnAuraZones();
    }

    // GroundZoneEffect(duration<=0)는 소유자가 죽으면 스스로 감지하고 풀에 반납되므로, 부활 시엔
    // 그냥 다시 스폰하면 된다 — 예전의 _auraCts 취소/재시작 관리가 필요 없어졌다.
    private void SpawnAuraZones()
    {
        foreach (GameObject prefab in auraZonePrefabs)
            SpawnGroundZone(prefab, transform.position, followOwner: true);
    }

    private static readonly object StatUpgradeBonusSource = new object();

    // ApplyTierLevelBonus와 같은 이유로 멱등해야 한다 — 풀링된 유닛을 재사용할 때(PrepareForSpawn)
    // 다시 호출되므로, source를 인스턴스(this)가 아니라 고정 오브젝트로 둬 재호출 시 이전 modifier를
    // 정확히 지우고 다시 얹을 수 있게 한다.
    private void ApplyStatUpgradeBonus()
    {
        sc.RemoveModifier(StatUpgradeBonusSource);
        if (upgradeState == null) return;

        float bonus = upgradeState.GetTotalEffect(statUpgrades);
        if (bonus == 0f) return;

        sc.AddModifier(StatType.ATK, new Modifier(ModifierType.Additive, bonus, 0f, StatLayer.Equip, StatUpgradeBonusSource));
        sc.AddModifier(StatType.DEF, new Modifier(ModifierType.Additive, bonus, 0f, StatLayer.Equip, StatUpgradeBonusSource));
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
        if (changedTier != Tier) return;
        ApplyTierLevelBonus();
        if (!isDead) currentHp = sc[StatType.HP]; // 업그레이드로 최대체력이 늘어난 만큼 낮이니 그냥 전부 채운다
    }

    private static readonly object ClassLevelBonusSource = new object();

    private void ApplyClassLevelBonus()
    {
        sc.RemoveModifier(ClassLevelBonusSource);
        int extraLevels = classUpgradeState.GetLevel(HeroType);
        if (extraLevels <= 0) return;

        foreach (var gain in classUpgradeState.GetStatGains(HeroType))
            sc.AddModifier(gain.statType, new Modifier(gain.modifierType, gain.amountPerLevel * extraLevels, 0f, StatLayer.Equip, ClassLevelBonusSource));
    }

    private void OnClassLevelChanged(int changedHeroType)
    {
        if (changedHeroType != HeroType) return;
        ApplyClassLevelBonus();
        if (!isDead) currentHp = sc[StatType.HP]; // 업그레이드로 최대체력이 늘어난 만큼 낮이니 그냥 전부 채운다
    }

    protected virtual void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.ChangeToDay -= Resurrection;
            gameManager.ChangeToDay -= HealFull;
            gameManager.ChangeToDay -= ResetSkillCooldown;
            gameManager.ChangeToDay -= NotifyDayStart;
        }
        tierUpgradeState.LevelChanged -= OnTierLevelChanged;
        classUpgradeState.LevelChanged -= OnClassLevelChanged;
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

    public void NotifyAttackResolved(AttackDataSO data)
    {
        for (int i = 0; i < traits.Length; i++) traits[i]?.OnAttackResolved(data);
    }

    public void NotifyHit(GameObject hitTarget, int amount, bool isCrit)
    {
        for (int i = 0; i < traits.Length; i++) traits[i]?.OnHit(hitTarget, amount, isCrit);
        if (hitTarget != null && hitTarget.GetComponentInParent<IDamageAble>() is IDamageAble d && d.Hp <= 0f)
            NotifyKill(hitTarget);
    }

    public void NotifyDayStart()
    {
        for (int i = 0; i < traits.Length; i++) traits[i]?.OnDayStart();
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

    // 체력바는 LateUpdate에서 굴린다 — 이동(Update)과 카메라 회전이 모두 끝난 뒤라야 빌보드가 한 프레임 밀리지 않는다.
    protected virtual void LateUpdate()
    {
        _bar.Tick(Hp, MaxHp, isDead);
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

    // AoE/장판 경로 전용 필터. mask가 None(기본값, 인자를 안 넘긴 호출부)이면 항상 통과 — 기존 동작 유지.
    // 그 외엔 AttackDataSO.AreaUnattackableTarget(Cloaking은 이미 빠진 마스크)과 적 속성을 대조한다.
    private static bool PassesAreaMask(GameObject enemy, EnemyAttribute mask)
    {
        if (mask == EnemyAttribute.None) return true;
        var eb = enemy.GetComponent<EnemyBase>();
        return eb == null || (eb.Attribute & mask) == 0;
    }

    // Enemy = AOE 스플래시용. areaUnattackableMask로 넘어온 속성(예: Fly)만 걸러내고, 그 외(은신 등)는
    // 그대로 맞는다 — 의도적으로 unattackableTarget 전체가 아니라 AreaUnattackableTarget만 적용한다.
    // TargetableEnemy = unattackableTarget 필터 적용(체인/멀티샷처럼 "특정 적을 타겟으로 선정"할 때).
    // Ally = 타일 점유자(OccupantObject) 기준 아군 조회. 영웅은 EnemyRegistry 같은 전역 리스트가
    // 없고 이미 타일당 1개 점유자 모델을 쓰고 있으므로 그 점유자를 훑는다(힐/피흡/힐 장판/오라용).
    // List + "이미 본 것" HashSet을 함께 써서 중복은 제거하되 타일 순회 순서는 유지한다 —
    // AttackTargetSelector.SelectTargets가 결과 리스트의 순서(pool[i % poolSize])에 의존하므로
    // HashSet 하나로만 중복 제거하면(순서 미보장) 멀티샷 대상 선정이 매 프레임 흔들릴 수 있다.
    public List<GameObject> GetObjectsInRange(Vector3 originWorld, int range, RangeShape shape,
        RangeQueryAffinity affinity = RangeQueryAffinity.Enemy, EnemyAttribute areaUnattackableMask = EnemyAttribute.None)
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
            {
                if (enemy == null || !seen.Add(enemy)) continue;
                bool passes = affinity == RangeQueryAffinity.TargetableEnemy
                    ? IsTargetable(enemy)
                    : PassesAreaMask(enemy, areaUnattackableMask);
                if (passes) found.Add(enemy);
            }

        return found;
    }

    public List<IDamageAble> GetEnemiesInLine(Vector3 originWorld, Vector3 towardWorld, int length, int width = 0, EnemyAttribute areaUnattackableMask = EnemyAttribute.None)
    {
        Vector2Int originCell = Board.WorldToCell(originWorld);
        Vector2Int dir = GridCalculator.CardinalToward(originCell, Board.WorldToCell(towardWorld));

        var found = new HashSet<IDamageAble>();

        if (Board.TryGetCell(originCell, out Tile originTile))
            foreach (GameObject enemy in originTile.Enemies)
                if (enemy != null && PassesAreaMask(enemy, areaUnattackableMask) && enemy.GetComponentInParent<IDamageAble>() is IDamageAble d)
                    found.Add(d);

        foreach (Tile tile in TileShapeQuery.GetLineTiles(Board, originCell, dir, length, width))
            foreach (GameObject enemy in tile.Enemies)
                if (enemy != null && PassesAreaMask(enemy, areaUnattackableMask) && enemy.GetComponentInParent<IDamageAble>() is IDamageAble d)
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
        bool wasDead = isDead;
        currentHp = sc[StatType.HP];
        isDead = false;
        _bar.ResetTo(currentHp, sc[StatType.HP]);
        if (wasDead) SpawnAuraZones(); // 살아있던 영웅은 오라가 이미 돌고 있으므로 다시 스폰하면 중복된다
    }

    public void HealFull()
    {
        Heal(sc[StatType.HP]);
    }

    // 풀에서 다시 꺼내 배치될 때 호출 — Awake/Start는 인스턴스 생애 최초 1회만 돌므로, 두 번째 이후
    // "삶"에 필요한 런타임 상태 초기화는 여기서 명시적으로 다시 해준다.
    public void PrepareForSpawn()
    {
        isDead = false;
        stateMachine.ChangeState(idleState);
        anim.SetBool(HeroAnimHash.idle, true);
        ApplyStatUpgradeBonus();
        ApplyTierLevelBonus();
        ApplyClassLevelBonus();
        currentHp = sc[StatType.HP];
        _bar.ResetTo(currentHp, sc[StatType.HP]);
        target = null;
        context.target = null;
        SpawnAuraZones();
    }

    // 풀로 돌려보내기 직전 호출 — 이번 삶에서 쌓인 상태를 걷어내 다음 삶으로 새어 들어가지 않게 한다.
    public void PrepareForDespawn()
    {
        HeroSelectionService.ClearIfSelected(this);
        SetSelected(false);
        isDead = true; // 오라 장판(GroundZoneEffect)이 다음 tick에서 owner.IsDead를 보고 스스로 풀에 반납한다
        buffManager.RemoveAllBuffs(this); // 다음 삶에 좀비 버프/디버프 수정자가 겹치지 않도록 정리
        debuffTracker.Clear();
        debuffEffects.Reset();
        skillCts?.Cancel();
        skillCts?.Dispose();
        skillCts = null;
    }

    public void SetCurrentTile()
    {
        origin = Board.WorldToCell(transform.position);
        if (Board.TryGetCell(origin, out Tile current))
            currentTile = current;
    }

    public void ExchangeAttackDatas(List<AttackDataSO> datas) => basePattern = datas;

}
