using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public abstract class EnemyBase : MonoBehaviour,IDamageAble,IUnit,IStunAble
{
    [SerializeField] protected string enemyKey;
    [SerializeField] protected List<SkillDataSO> skills = new();
    [Tooltip("은신(Cloaking) 공용 설정. IsCloaking일 때만 사용 — 걸을 땐 은신 재질, 저지 시 원래 재질.")]
    [SerializeField] private CloakSettingsSO cloakSettings;
    [Tooltip("기본 공격 애니 클립의 원래 길이(초). 공속이 빨라져 공격 간격(1/AS)이 이 값보다 짧아지면 애니를 그만큼 배속한다. 0이면 배속하지 않음.")]
    [SerializeField] private float attackClipLength = 0f;
    [Tooltip("잠행(Burrow) 중 지면에 표시할 마커 이펙트(흙더미/먼지 등). IsBurrow일 때만 사용. 비우면 마커 없이 숨는다.")]
    [SerializeField] private GameObject burrowMarkerPrefab;
    [Tooltip("파고들기/솟아오르기 애니 이벤트가 안 왔을 때 강제로 다음 상태로 넘기는 시간(초). 클립 길이보다 넉넉하게.")]
    [SerializeField] private float burrowTimeout = 3f;
    [Tooltip("적 머리 위 체력바. 없는 프리팹이면 비워두면 된다(체력바 로직 전체가 no-op).")]
    public Slider healthSlider;
    [Tooltip("체력바가 현재 체력을 따라가는 속도. 클수록 빠르게 붙는다.")]
    private float sliderSpeed = 10f;
    [Tooltip("체력바 패널에 위에 보일 보스 이름")] //일반 엘리트는 일단 없음(추후 고민)
    public TMP_Text bossName;
    [Tooltip("디버프 아이콘이 소환될 부모. GridLayoutGroup이 달린 오브젝트를 꽂는다. 비우면 디버프 아이콘 로직 전체가 no-op.")]
    public RectTransform debuffIconRoot;
    [Tooltip("디버프 아이콘 1칸 프리팹. Image 컴포넌트가 있어야 하고, 스프라이트는 debuffIcons에서 종류별로 지정한다.")]
    public GameObject debuffIconPrefab;
    [Tooltip("종류별 아이콘 스프라이트. 여기 등록되고 스프라이트가 들어 있는 종류만 표시된다.")]
    public EnemyDebuffIcon[] debuffIcons;
    private float[] skillTimers;
    private bool[] skillRunning;
    private CancellationTokenSource skillCts;
    private CancellationTokenSource[] skillCtsPer; // 스킬별 취소 토큰(skillCts에 연결) — 스턴 시 액티브 스킬만 개별 취소
    // 유닛 생존 동안 유효한 취소 토큰(OnDisable에서 취소). Heal처럼 Execute보다 오래 사는
    // fire-and-forget 효과는 per-skill 토큰(Execute 종료 시 dispose됨)이 아니라 이걸 써야 디스폰 시 정상 취소된다.
    public CancellationToken LifetimeToken => skillCts != null ? skillCts.Token : CancellationToken.None;
    [field: SerializeField] public float Hp { get; protected set; }   // 인스펙터 표시용(런타임 값 확인). 값은 ApplyData/재생/피격이 갱신.
    // 아래 스탯들은 StatContainer(sc)에서 파생 — 값/버프는 sc가 단일 소스. ApplyData가 sc를 채운 뒤부터 유효.
    public float MaxHp => sc[StatType.HP];
    public int Defense => Mathf.RoundToInt(sc[StatType.DEF]);
    public int AttackPower => Mathf.RoundToInt(sc[StatType.ATK]);
    public float AttackSpeed => sc[StatType.AS];
    public int Range { get; protected set; }
    public float MoveSpeed => sc[StatType.SPD];
    public EnemyType Type { get; protected set; }        // 근거리/원거리
    public EnemyClass Class { get; protected set; }      // 일반/엘리트/보스
    public EnemyAttribute Attribute { get; protected set; } // 외부에서 볼수있는 특성
    private EnemyAttribute Dataattribute {get; set;} //원본
    public bool IsCloaking => (Dataattribute & EnemyAttribute.Cloaking) != 0; // 은신
    public bool IsFly      => (Dataattribute & EnemyAttribute.Fly)      != 0; // 공중
    public bool IsUnJudged => (Dataattribute & EnemyAttribute.UnJudged) != 0; // 저지 불가
    public bool IsBerserk => (Dataattribute & EnemyAttribute.Berserk) != 0; //폭주
    public bool IsHitsShield => (Dataattribute & EnemyAttribute.HitsShield) != 0; // 타수 보호막
    public bool IsRegeneration => (Dataattribute & EnemyAttribute.Regeneration) != 0; // 재생
    public bool IsBurrow => (Dataattribute & EnemyAttribute.Burrow) != 0; // 잠행: 숨어 이동, 저지 시 솟아올라 공격
    public bool IsDead { get; protected set; }
    public bool IsSpawnInvincible = false;
    private StatContainer sc = new();
    public StatContainer Stats => sc;
    public Animator animator;
    private bool _attacking; // 공격 모션 재생 중 — 이 동안 스킬 시전을 막아 애니메이터 충돌 방지
    private EnemyMovement _move; // 경로 추종 이동 — Awake에서 생성, 아래 API는 여기로 위임
    private PoolManager _pool;
    private GameManager gameManager;
    public GameManager GameManager => gameManager;
    private bool firstEnable =false;
    [Inject] 
    public void Construct(PoolManager pool,WaveSpawner waveSpawner,GameManager gameManager)
    {
        _pool = pool;
        this.waveSpawner = waveSpawner;
        this.gameManager = gameManager;
    }
    protected PoolManager Pool => _pool ??= PoolManager.Instance; // 파생 클래스(Bat 등)도 재사용
    
    private WaveSpawner waveSpawner;
    // 이 적이 속한 스포너(레인). 스폰 시 스포너가 SetOwner로 주입 → 죽거나 본진 도달 시 그 스포너의 카운트만 감소.
    // 분열체는 부모의 Owner를 그대로 물려받아 같은 레인 카운트에 반영된다.
    public WaveSpawner Owner => waveSpawner;
    public void SetOwner(WaveSpawner spawner) => waveSpawner = spawner;

    // 이번 생존 동안 웨이브 카운트를 이미 내렸는지. 사망/본진 도달 경로가 모두 SendDieEvent를 타므로
    // 이 플래그가 이중 감소를 막는다(이중 감소하면 적이 남았는데 EnemyAllClear가 먼저 터져 웨이브가 앞서간다).
    private bool _dieEventSent;

    // 이 유닛이 판에서 빠졌음을 스포너에 딱 한 번 알린다.
    // 카운트를 못 내리면 Enemycount가 0에 닿지 않아 EnemyAllClear가 영영 안 터지므로(웨이브 정지),
    // 사망 처리가 어떤 경로로 끝나든(정상/애니 타임아웃/취소) 반드시 여기를 지나가야 한다.
    // Owner가 없는 적(씬에 직접 배치, SpawnerTest 등)도 있으므로 null 조건 호출.
    private void SendDieEvent()
    {
        if (_dieEventSent) return;
        _dieEventSent = true;
        waveSpawner?.EnemyDieEvent();
    }

    private bool AnySkillRunning()
    {
        if (skillRunning == null) return false;
        for (int i = 0; i < skillRunning.Length; i++)
            // 배경 오라(BlocksBasicAttack=false)는 계속 실행 중이어도 일반 공격을 막지 않는다.
            if (skillRunning[i] && skills[i] != null && skills[i].BlocksBasicAttack) return true;
        return false;
    }

    [Header("Movement")]
    [Tooltip("웨이포인트 도달 판정 거리의 제곱(작을수록 정확). 기본값 유지 권장.")]
    [SerializeField] private float arriveSqr = 0.0004f;

    public MapBoard Board => _move.Board;
    public bool HasPath => _move.HasPath;
    public IReadOnlyList<Vector3> Path => _move.Path;
    public int PathIndex => _move.PathIndex;
    public bool MovementSuspended { get => _move.Suspended; set => _move.Suspended = value; }
    public void ResumeFrom(int index) => _move.ResumeFrom(index);
    public void ResumeFromNearest() => _move.ResumeFromNearest();
    public Vector3 PointAhead(float dist, out int landIndex) => _move.PointAhead(dist, out landIndex);
    private float _damageBlock;   // 고정 감소 수치
    private float _shieldExpiry;  // Time.time 기준 만료 시각 — 코루틴 없이 지연 만료(풀링 안전)

    private bool _berserkOn;        // 이번 생존 동안 이미 발동했는지(중복 누적 방지)
    private Vector3 _baseScale;     // 원래 스케일 — 풀 재사용 시 여기로 복구(분열체가 줄여놓은 걸 리셋)
    public bool IsShielded => Time.time < _shieldExpiry;

    private float _stunExpiry; 
    private bool _stunAnimActive;
    private bool _hasStunParam;
    private bool _stunParamChecked;
    public bool IsStunned => Time.time < _stunExpiry;
    protected virtual void Awake()
    {
        _baseScale = transform.localScale; // 프리팹 원래 스케일 스냅샷(분열 축소 후 복구 기준)
        _bar.Setup(healthSlider, sliderSpeed); // LoadStats(→ApplyData)가 바를 채우므로 그보다 먼저
        _debuffs.Setup(debuffIconRoot, debuffIconPrefab, debuffIcons); // 아이콘은 디버프가 걸릴 때 풀에서 소환된다
        LoadStats();
        animator = GetComponent<Animator>();
        _move = new EnemyMovement(gameObject, animator, arriveSqr);
        // 잠행 몹은 은신 셰이더 페이드를 쓰지 않는다(연출을 EnemyBurrow가 전담) — Cloaking 비트는 피격 판정용으로만 남긴다.
        if (IsCloaking && !IsBurrow) _cloak.Setup(gameObject, cloakSettings); // Attribute 결정(LoadStats) 뒤에 호출
        if (IsBurrow) _burrow.Setup(gameObject, animator, burrowMarkerPrefab, burrowTimeout);
    }

    protected virtual void OnEnable()
    {
        LoadStats();
        _attacking = false;
        _shieldExpiry = 0f;
        _stunExpiry = 0f;               // 풀 재사용 시 이전 스턴 잔여 제거
        _stunAnimActive = false;        // animator.Rebind()로 bool도 초기화되므로 상태만 맞춰둔다
        _berserkOn = false;
        _dieEventSent = false;          // 풀 재사용 시 새 생존 시작 — 카운트를 다시 한 번 내릴 수 있게
        SplitGeneration = 0;
        transform.localScale = _baseScale; 
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
        EnemyRegistry.Register(this);
        EnemyArchiveData.Unlock(enemyKey);   // 등장 = 도감 해금 (멱등 — 재등장해도 최초 1회만 저장)
        _move.Resume();
        _cloak.Reset();
        _burrow.Reset();   // animator.Rebind() 뒤라 Burrowed bool이 유지된다(스폰 = 숨은 상태로 시작)
        skillCts = new CancellationTokenSource();
        _move.ArrivedAtCore += HandleArrivedAtCore;
        RunSkillLoop(skillCts.Token).Forget();
        RunAttackLoop(skillCts.Token).Forget();
        if(IsRegeneration)Regeneration(skillCts.Token).Forget();
    }
    protected virtual void OnDisable()
    {
        // 죽은 채로 비활성화되면 여기서 카운트를 내린다. 비활성화는 동기 실행이라 풀 재사용과 겹칠 수 없고,
        // DieRoutine의 취소가 끝내 관측되지 않는 경우(씬 언로드, 도메인 리로드)까지 덮는다.
        // 본진 도달은 IsDead가 아니므로 여기 안 걸리고, 이미 보낸 경우는 SendDieEvent가 무시한다.
        if (IsDead) SendDieEvent();
        _cloak.Reset();
        _burrow.Reset();   // 지면 마커를 풀에 반납(안 하면 적에 딸려가 재사용 시 되살아난다)
        _bar.Reset();
        _debuffs.Reset();  // 소환된 아이콘을 풀에 반납(안 하면 다음 스폰이 이전 개체의 디버프 아이콘을 물고 나온다)
        _move.Pause();
        _move.LeaveBoard(); // 어떤 경로로 사라지든 현재 칸에서 빠진다
        _move.ArrivedAtCore -= HandleArrivedAtCore;
        sc.RemoveModifier(this);
        skillCts?.Cancel();
        skillCts?.Dispose();
        skillCts = null;
    }
    public void EnterMap(MapBoard board, IReadOnlyList<Vector3> waypoints = null, bool snapToStart = true)
    {
        _move.Flying = IsFly; // 공중 특성이면 지형 무시(본진으로 직선). Map/길찾기는 건드리지 않음
        _move.EnterMap(board, waypoints, snapToStart, MoveSpeed, enemyKey);
    }

    // 재생 특성 회복량(초당 최대체력 비율). 매 프레임 deltaTime만큼 나눠 채워 부드럽게 차오른다.
    private const float RegenPerSecond = 0.005f;
    // 은신 렌더링은 EnemyCloak가 전담. 은신 몹이면 Awake에서 Setup, 매 프레임 Tick으로 굴린다.
    private readonly EnemyCloak _cloak = new();
    // 머리 위 체력바는 EnemyHealthBar가 전담(빌보드·보간·표시 여부). 프리팹에 Slider가 없으면 통째로 no-op.
    private readonly EnemyHealthBar _bar = new();
    // 잠행 연출은 EnemyBurrow가 전담(Burrowed bool·렌더러 on/off·지면 마커). 잠행 몹이 아니면 통째로 no-op.
    private readonly EnemyBurrow _burrow = new();
    // 체력바 아래 디버프 아이콘은 EnemyDebuffBar가 전담. 아이콘을 안 꽂은 프리팹이면 통째로 no-op.
    private readonly EnemyDebuffBar _debuffs = new();
    // 모디파이어가 붙기 전 원본 스탯값. ApplyData가 sc에 넣는 값을 그대로 여기에도 기록한다 —
    // StatContainer가 base를 되읽는 API를 주지 않으므로(팀원 소유 파일), 디버프 판정 기준을 이쪽에서 들고 있어야 한다.
    private readonly Dictionary<StatType, float> baseStats = new();

    protected virtual void Update()
    {
        // active=사망/스턴 아님 → 스턴 중엔 이동 정지(Idle).
        // 잠행 몹은 파고들기/솟아오르기 모션 중에도 멈춘다 — 안 그러면 걸어가면서 땅을 파고 솟는 게 보인다.
        _move.Tick(!IsDead && !IsStunned && !_burrow.IsTransitioning, MoveSpeed);
        UpdateExposedAttribute();       // 저지 상태에 따라 Hero가 보는 Attribute를 갱신
        _cloak.Tick(CloakClear);
        _burrow.Tick(CloakClear, transform.position); // 은신과 같은 트리거(저지/사망) — 저지되면 솟아오른다
        StunTick();                     // 스턴 만료를 감지해 Animator bool을 끈다
    }

    // 체력바는 LateUpdate에서 굴린다 — 이동(Update)과 카메라 회전(CameraInput.Update)이 모두 끝난 뒤라야
    // 빌보드가 한 프레임 밀리지 않는다.
    protected virtual void LateUpdate()
    {
        // Attribute는 UpdateExposedAttribute가 매 프레임 갱신하므로(저지 중이면 Cloaking 비트가 빠짐)
        // "체력바가 보이는 것 == 영웅이 때릴 수 있는 것"이 항상 일치한다.
        // 표시 여부(안 맞았으면 숨김 / 죽을 땐 0까지 깎이는 걸 보여줌)는 EnemyHealthBar가 판단한다.
        bool cloakedNow = (Attribute & EnemyAttribute.Cloaking) != 0;
        _bar.Tick(Hp, MaxHp, IsDead, cloakedNow,Class);
        _debuffs.Tick(sc, baseStats, IsStunned);
    }

    // 외부(영웅 등)에서 이 적을 duration초간 스턴. 이동/공격/스킬 시전이 모두 멈춘다.
    // 이미 걸린 스턴보다 긴 스턴이 들어오면 만료 시각을 갱신(중첩 시 최댓값). 애니 bool은 StunTick이 켠다.
    public void Stun(float duration)
    {
        if (IsDead || duration <= 0f) return;
        bool wasStunned = IsStunned;
        float expiry = Time.time + duration;
        if (expiry > _stunExpiry) _stunExpiry = expiry;
        if (!wasStunned) InterruptActiveSkills(); // 스턴 시작(상승 엣지)에만 끊기 — 재스턴 시 중복 취소 방지
    }

    // 스턴 시작 시 호출 — 애니를 재생하는 액티브 스킬(BlocksBasicAttack=true)만 즉시 취소한다.
    // 배경 오라(BlocksBasicAttack=false, 지속형)는 끊지 않고 계속 유지.
    private void InterruptActiveSkills()
    {
        if (skillCtsPer == null) return;
        for (int i = 0; i < skillCtsPer.Length; i++)
            if (skillRunning[i] && skills[i] != null && skills[i].BlocksBasicAttack)
                skillCtsPer[i]?.Cancel();
    }

    // 스턴 상태를 Animator에 반영 — 바뀐 프레임에만 처리(만료 시 자동 해제도 여기서).
    // Stun bool 파라미터가 있으면 그걸로(Stun 스테이트가 연출 담당), 없으면 애니를 얼려서 "굳음"으로 대체.
    private void StunTick()
    {
        bool stunned = IsStunned;
        if (_stunAnimActive == stunned) return;
        _stunAnimActive = stunned;
        if (animator == null) return;

        if (HasStunParam())
            animator.SetBool("Stun", stunned);   // Stun 스테이트가 연출을 담당(권장)
        else
            animator.speed = stunned ? 0f : 1f;  // fallback: 파라미터 없으면 현재 프레임에서 얼림 → 풀리면 원복
    }
    
    // 애니메이터에 Bool "Stun" 파라미터가 있는지 1회 검사 후 캐시(파라미터 목록은 런타임에 안 바뀜).
    private bool HasStunParam()
    {
        if (_stunParamChecked) return _hasStunParam;
        _stunParamChecked = true;
        foreach (var p in animator.parameters)
            if (p.type == AnimatorControllerParameterType.Bool && p.name == "Stun") { _hasStunParam = true; break; }
        return _hasStunParam;
    }

    // Hero가 읽는 공개 Attribute 갱신.
    // 은신 유닛이 저지당하는 동안엔 Cloaking 비트를 빼서 Hero의 unattackable 필터를 통과(=공격 가능)시킨다.
    // 저지가 풀리면 다시 원본으로 돌아가 공격 불가. 연출(EnemyCloak)과 동일한 IsBlocked 조건이라 "보이는 것=때릴 수 있는 것"이 항상 일치.
    private void UpdateExposedAttribute()
    {
        EnemyAttribute exposed = Dataattribute;
        // 잠행 몹은 "다 솟아오른 뒤"에만 노출한다 — 아직 땅속인데 때릴 수 있으면
        // "보이는 것 == 때릴 수 있는 것" 불변식이 깨진다(셰이더 페이드일 땐 저절로 맞았지만 물리 연출은 아니다).
        if (IsCloaking && Board != null && Board.IsBlocked(gameObject)
            && (!IsBurrow || _burrow.IsSurfaced))
            exposed &= ~EnemyAttribute.Cloaking;
        Attribute = exposed;
    }

    // 저지/사망이면 또렷, 아니면 은신으로 페이드. 실제 렌더링은 EnemyCloak가 처리.
    private bool CloakClear => IsDead || (Board != null && Board.IsBlocked(gameObject));



    // 초당 MaxHp*RegenPerSecond를 프레임 단위로 나눠 회복 → 1초마다 툭툭 차는 게 아니라 연속으로 차오름.

    // 본진 도달 시 EnemyMovement가 이벤트로 호출. 도달 후 처리·디스폰은 본체가 쥔다.
    private void HandleArrivedAtCore()
    {
        OnArrivedAtCore();
        if (this != null && gameObject != null) Pool.Despawn(gameObject);
    }

    protected virtual void OnArrivedAtCore()
    {
        SendDieEvent();
        var gm = gameManager;
        if (gm == null)
            Debug.LogWarning($"[{name}] GameManager를 찾을 수 없음 — HpDamage 스킵.", this);
        else
            gm.HpDamage(Class);
        if (Board != null) Board.RemoveEnemy(gameObject);
    }
    private async UniTask RunSkillLoop(CancellationToken token)
    {
        if (skills == null || skills.Count == 0) return;
        skillTimers = new float[skills.Count];
        skillRunning = new bool[skills.Count];
        skillCtsPer = new CancellationTokenSource[skills.Count];

        while (!IsDead)
        {
            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i] == null || skillRunning[i] || skills[i].TriggerOnDeath) continue; // 온데스 스킬은 Die()에서만 발동
                skillTimers[i] += Time.deltaTime;
                // 공격 모션 중이면 시전 보류(타이머는 계속 쌓여서 공격 끝나면 바로 발동).
                if (skillTimers[i] >= skills[i].cooldown && !_attacking && !IsStunned)
                {
                    RunSkill(i, token).Forget();
                }
            }
            await UniTask.Yield(token);
        } 
        
    }
    public virtual void EnemySoundAttack()
    {

    }

    // ---- 잠행 Animation Event ----
    // 각 클립 마지막 프레임에 Animation Event로 이 메서드 이름을 걸어준다.
    // 안 걸어도 EnemyBurrow의 타임아웃이 강제로 넘겨주지만(경고 로그), 연출 타이밍이 어긋난다.
    public void AnimEvent_Burrowed() => _burrow.NotifyBurrowed();   // 파고들기 끝 → 렌더러 off
    public void AnimEvent_Surfaced() => _burrow.NotifySurfaced();   // 솟아오르기 끝 → 공격 허용

    private async UniTask RunSkill(int index, CancellationToken token)
    {
        skillRunning[index] = true;
        // 스킬별 CTS(상위 token에 연결) — 스턴 시 이 스킬만 개별 취소할 수 있게(지속형 오라는 건드리지 않음).
        var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        skillCtsPer[index] = cts;
        try { await skills[index].Execute(this, cts.Token); }
        catch (System.OperationCanceledException) { /* 비활성/파괴/스턴으로 취소 — 정상 */ }
        finally
        {
            skillRunning[index] = false;
            skillTimers[index] = 0f;
            skillCtsPer[index] = null;
            cts.Dispose();
        }
    }

    private async UniTask RunAttackLoop(CancellationToken token)
    {
        float attackTimer = 0f;
        while (!IsDead)
        {
            float interval = AttackSpeed > 0f ? 1f / AttackSpeed : 1f; //attackspped = 초당 공격횟수
            // 공격 모션/스킬 시전 중엔 딜레이를 세지 않는다 — 공격이 끝난 뒤부터 interval을 새로 채워
            // 매 공격 사이에 온전한 간격을 보장(안 그러면 모션 중 타이머가 넘쳐 애니 끝나자마자 연사됨).
            if (!_attacking && !AnySkillRunning() && !IsStunned)
            {
                attackTimer += Time.deltaTime;
                if (attackTimer >= interval)
                {
                    attackTimer = 0f;
                    Attack();
                }
            }
            await UniTask.Yield(token);
        }
    }
    protected void LoadStats()
    {
        if (string.IsNullOrEmpty(enemyKey)) enemyKey = GetType().Name;

        var data = EnemyStatLoader.Load(enemyKey);
        if (data == null) return;

        ApplyData(data);
        EnemyStatLoader.ResolveSkills(data.Skills, skills);
    }
    
    protected virtual void ApplyData(EnemyTable.Data data)
    {
        if(!firstEnable||gameManager==null)
        {
            firstEnable = true;
            sc.AddStat(StatType.HP,data.Health);
            sc.AddStat(StatType.ATK,data.Attack);
            sc.AddStat(StatType.AS,data.AttackSpeed);
            sc.AddStat(StatType.DEF,data.Defense);
            sc.AddStat(StatType.SPD,data.MoveSpeed);
            RecordBaseStats(data.Health, data.Attack, data.AttackSpeed, data.Defense, data.MoveSpeed);
            Type = ParseEnum(data.Type, EnemyType.Melee);
            Class = ParseEnum(data.Class, EnemyClass.Normal); 
            Attribute = ParseAttribute(data.Attribute);
            Dataattribute = Attribute;
        }
        else
        {
            // 날짜 스케일이 붙는 HP/DEF는 지역변수로 한 번만 계산한다 — baseStats에 다른 값이 들어가면 디버프 판정이 어긋난다.
            float scaledHp  = data.Health  + (gameManager.DayCount * data.UpHealthScale);
            float scaledDef = data.Defense + (data.UpDefenseScale * (gameManager.DayCount / 5));
            sc.SetBaseValue(StatType.HP,scaledHp);
            sc.SetBaseValue(StatType.ATK,data.Attack);
            sc.SetBaseValue(StatType.AS,data.AttackSpeed);
            sc.SetBaseValue(StatType.DEF,scaledDef);
            sc.SetBaseValue(StatType.SPD,data.MoveSpeed);
            RecordBaseStats(scaledHp, data.Attack, data.AttackSpeed, scaledDef, data.MoveSpeed);
        }
        Range = data.Range;
        Hp = sc[StatType.HP];
        _bar.ResetTo(Hp, MaxHp); // 스폰 시 보간 없이 즉시 풀피로(풀 재사용 시 이전 값 잔상 제거)
        IsDead = false;
        if(bossName == null)return;
        bossName.text = DataTableManager.StringTable.Get(data.Name); 
        //MoveSpeed = data.MoveSpeed;
    }

    // 방금 sc에 넣은 원본값을 그대로 장부에 남긴다. EnemyDebuffBar가 "지금 값이 이보다 낮은가"로 디버프를 판정한다.
    // 인자를 받는 이유는 호출부에서 sc에 넣은 것과 같은 식(式)을 쓰도록 강제하기 위함 — 여기서 다시 계산하면 어긋날 수 있다.
    private void RecordBaseStats(float hp, float atk, float attackSpeed, float def, float spd)
    {
        baseStats[StatType.HP]  = hp;
        baseStats[StatType.ATK] = atk;
        baseStats[StatType.AS]  = attackSpeed;
        baseStats[StatType.DEF] = def;
        baseStats[StatType.SPD] = spd;
    }

    // CSV 문자열 → enum. 비었거나 못 읽으면 fallback으로 대체(대소문자 무시).
    private static T ParseEnum<T>(string raw, T fallback) where T : struct, System.Enum
    {
        if (!string.IsNullOrEmpty(raw) && System.Enum.TryParse(raw.Trim(), true, out T value))
            return value;
        return fallback;
    }
    private static EnemyAttribute ParseAttribute(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return EnemyAttribute.None;
        EnemyAttribute result = EnemyAttribute.None;
        foreach (string token in raw.Split(new[] { '|', ';' }, StringSplitOptions.RemoveEmptyEntries))
            if (System.Enum.TryParse(token.Trim(), true, out EnemyAttribute flag))
                result |= flag;
        return result;
    }


    public void ApplyDamageReductionShield(float flatReduce, float duration)
    {
        _damageBlock = Mathf.Max(0f, flatReduce);
        _shieldExpiry = Time.time + duration;
        Debug.Log($"[Shield] 쉴드 부여 시점 t={Time.time:F2} block={_damageBlock} expiry={_shieldExpiry:F2}", this);
    }

    public void TakeDamage(int damage)
    {
        if(IsDead)return;
        if(IsSpawnInvincible)return;
        int reduce = Defense + (IsShielded ? Mathf.RoundToInt(_damageBlock) : 0);
        int hitDamage = Mathf.Max(1, damage - reduce);
        if(IsHitsShield)
        {
            Hp -= 1f;
        }
        else
        {
            Hp -= hitDamage;
        }                     
        if (!_berserkOn && Hp < MaxHp * 0.5f && IsBerserk)
        {
            _berserkOn = true;
            // MoveSpeed += 3f;
            // AttackPower += 10;
            sc.AddModifier(StatType.SPD,new Modifier(ModifierType.Flat,3f,0f,StatLayer.Equip,this));
            sc.AddModifier(StatType.ATK,new Modifier(ModifierType.Flat,10f,0f,StatLayer.Equip,this));
        }
        if(Hp<=0)Die();
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;
        Hp = Mathf.Min(Hp + amount, MaxHp);
    }
    private async UniTask Regeneration(CancellationToken token)
    {
        while(!IsDead)
        {
            if(Hp<MaxHp)
            Hp = Mathf.Min(Mathf.Max(1f,Hp + MaxHp * RegenPerSecond * Time.deltaTime), MaxHp);
            await UniTask.Yield(token);
        }
    }

    // 분열 등에서 스폰 직후 현재 체력을 물려줄 때 사용.
    // 스폰 시 OnEnable이 LoadStats로 풀피 리셋하므로, 그 뒤에 이걸 호출해 현재 체력으로 덮어쓴다.
    public void SetCurrentHp(float hp) => Hp = Mathf.Clamp(hp, 1f, MaxHp);

    // 분열 세대. 0=원본, 분열체는 부모+1. 무한 분열 방지용(SplitSkill이 maxGeneration으로 제한).
    public int SplitGeneration { get; private set; }

  

    public void SetSplitGeneration(int gen) => SplitGeneration = gen;

    // 스폰 직후 인스턴스 스케일을 원래 크기의 mul배로 설정(분열체 축소용). OnEnable에서 매 스폰 원복되므로 풀 재사용 안전.
    public void SetScaleMul(float mul) => transform.localScale = _baseScale * mul;
    public virtual void Attack()
    {
        if (IsDead || Board == null || skillCts == null) return;
        GameObject target = FindAttackTarget();
        if (target == null) return; // 사거리에 대상 없으면 멈추지도, 공격하지도 않음
        if (IsUnJudged) return;                                              // 저지 불가 = 막는 칸을 통과만, 스쳐 지나가며 때리지 않음
        if (Type == EnemyType.Melee && !Board.IsBlocked(gameObject)) return; // 근접은 실제로 저지당했을 때만 공격
        if (IsBurrow && !_burrow.IsSurfaced) return;                         // 잠행: 다 솟아오르기 전엔 때리지 않는다

        transform.LookAt(target.transform);
        _attacking = true;
        _move.Pause();
        if (animator != null)
        {
            // 공속이 빨라져 공격 간격(1/AS)이 클립 길이보다 짧아지면 그 비율로 애니를 압축(배속).
            // 간격이 더 길 땐 1배속 유지 — 억지로 늘려 슬로우모션처럼 보이는 걸 방지.
            float interval = AttackSpeed > 0f ? 1f / AttackSpeed : 1f;
            animator.speed = (attackClipLength > interval && interval > 0f) ? attackClipLength / interval : 1f;
            animator.SetTrigger("Attack");
        }
        AttackWatchdog(skillCts.Token).Forget(); // 애니 끝나면 상태 복구(이벤트 누락 대비 타임아웃 포함)
    }

    
    public virtual void AnimEvent_AttackHit()
    {
        if (IsDead) return;
        GameObject target = FindAttackTarget(); 
        if (target != null && target.GetComponentInParent<IDamageAble>() is IDamageAble dmg)
        {
            EnemySoundAttack();
            dmg.TakeDamage(AttackPower);
        }
            
    }
    protected GameObject FindAttackTarget()
    {
        if (Board == null) return null;
        Vector2Int origin = Board.WorldToCell(transform.position);
        GameObject target = null;
        int bestDist = int.MaxValue;
        foreach (Tile tile in Board.GetTiles(origin, Range))
        {
            if (tile.OccupantObject == null) continue;
            int d = EnemyTargeting.Distance(origin, tile.Coord);
            // if(tile.OccupantObject.GetComponent<Hero>().IsDead) continue;
            if (tile.OccupantObject.GetComponent<Hero>() is not Hero h || h.IsDead) continue;
            if (d < bestDist) { bestDist = d; target = tile.OccupantObject; }
        }
        return target;
    }

    private async UniTask AttackWatchdog(CancellationToken token)
    {
        try { await WaitForAttackAnim("Attack", 5f, token); }
        catch (OperationCanceledException) { }
        finally
        {
            if (animator != null) animator.speed = 1f; // 공격 배속 원복(전역 speed이므로 이동/사망 애니에 안 새게)
            _attacking = false; // 공격 모션 끝 → 스킬/다음 공격 허용
            _move.Resume();
        }
    }
    private async UniTask WaitForAttackAnim(string stateName,float timeout,CancellationToken token)
    {
        const int layer = 0;
        float elapsed = 0f;
        while (animator != null && !animator.GetCurrentAnimatorStateInfo(layer).IsName(stateName))
        {
            elapsed += Time.deltaTime;
            if (elapsed >= timeout)
            {
                Debug.LogWarning($"[{name}] Animator에서 '{stateName}' 스테이트를 찾지 못함 — 이름/전이 확인.", this);
                return;
            }
            await UniTask.Yield(token);
        }
        if (animator == null) return;

        var info = animator.GetCurrentAnimatorStateInfo(layer);
        await UniTask.Delay(TimeSpan.FromSeconds(info.length / Mathf.Max(0.01f, animator.speed)), cancellationToken: token);
    }

    public virtual void Die()
    {
        if (IsDead) return;
        if (animator != null) animator.speed = 1f; // 애니메이터 없는 적에서 여기서 터지면 IsDead도 못 세우고 영영 안 죽는다
        IsDead = true;
        _move.Stop();
        if (Board != null) Board.RemoveEnemy(gameObject);
        // 이미 비활성(OnDisable로 skillCts 해제)이면 사망 연출을 기다릴 수 없다 → 카운트만 내리고 즉시 반납.
        // 온데스 스킬은 여기서 발동시키지 않는다(비활성 상태에서 분열체를 스폰하면 자리/경로가 없다).
        if (skillCts == null) { SendDieEvent(); Despawn(); return; }
        DieRoutine(skillCts.Token).Forget();
    }

    private void TriggerDeathSkills()
    {
        if (skills == null) return;
        for (int i = 0; i < skills.Count; i++)
            if (skills[i] != null && skills[i].TriggerOnDeath)
                skills[i].Execute(this, CancellationToken.None).Forget();
    }
    private async UniTask DieRoutine(CancellationToken token)
    {
        try
        {
            if (animator != null) animator.SetTrigger("Die");
            await WaitForDeathAnim("Die", 5f, token);
        }

        catch (OperationCanceledException) { return; }

        TriggerDeathSkills();
        SendDieEvent();
        Despawn();

    }
    private async UniTask WaitForDeathAnim(string stateName, float timeout, CancellationToken token)
    {
        const int layer = 0;
        float elapsed = 0f;
        while (animator != null && !animator.GetCurrentAnimatorStateInfo(layer).IsName(stateName))
        {
            elapsed += Time.deltaTime;
            if (elapsed >= timeout)
            {
                Debug.LogWarning($"[{name}] Animator에서 '{stateName}' 스테이트를 찾지 못함 — 이름/전이 확인.", this);
                return;
            }
            await UniTask.Yield(token);
        }
        if (animator == null) return;

        var info = animator.GetCurrentAnimatorStateInfo(layer);
        await UniTask.Delay(TimeSpan.FromSeconds(info.length / Mathf.Max(0.01f, animator.speed)), cancellationToken: token);
    }

    
    protected virtual void Despawn()
    {
        if (this != null && gameObject != null) Pool.Despawn(gameObject);
    }

}