using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

using UnityEngine;
using VContainer;

public abstract class EnemyBase : MonoBehaviour,IDamageAble,IUnit
{
    [SerializeField] protected string enemyKey;
    [SerializeField] protected List<SkillDataSO> skills = new();
    [Tooltip("은신(Cloaking) 공용 설정. IsCloaking일 때만 사용 — 걸을 땐 은신 재질, 저지 시 원래 재질.")]
    [SerializeField] private CloakSettingsSO cloakSettings;
    [Tooltip("기본 공격 애니 클립의 원래 길이(초). 공속이 빨라져 공격 간격(1/AS)이 이 값보다 짧아지면 애니를 그만큼 배속한다. 0이면 배속하지 않음.")]
    [SerializeField] private float attackClipLength = 0f;

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

    private float _stunExpiry;      // Time.time 기준 스턴 만료 시각 — 코루틴 없이 지연 만료(풀링 안전, Shield와 동일 패턴)
    private bool _stunAnimActive;   // Animator에 보고한 마지막 스턴 상태 — 바뀐 프레임에만 SetBool("Stun") 호출
    private bool _hasStunParam;     // 애니메이터에 Bool "Stun" 파라미터가 있는지(1회 검사 후 캐시)
    private bool _stunParamChecked;
    public bool IsStunned => Time.time < _stunExpiry; // 스턴 중엔 이동/공격/스킬 시전이 모두 멈춘다
    protected virtual void Awake()
    {
        _baseScale = transform.localScale; // 프리팹 원래 스케일 스냅샷(분열 축소 후 복구 기준)
        LoadStats();
        animator = GetComponent<Animator>();
        _move = new EnemyMovement(gameObject, animator, arriveSqr);
        if (IsCloaking) _cloak.Setup(gameObject, cloakSettings); // Attribute 결정(LoadStats) 뒤에 호출
    }

    protected virtual void OnEnable()
    {
        LoadStats();
        _attacking = false;
        _shieldExpiry = 0f;
        _stunExpiry = 0f;               // 풀 재사용 시 이전 스턴 잔여 제거
        _stunAnimActive = false;        // animator.Rebind()로 bool도 초기화되므로 상태만 맞춰둔다
        _berserkOn = false;             
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
        skillCts = new CancellationTokenSource();
        _move.ArrivedAtCore += HandleArrivedAtCore;
        RunSkillLoop(skillCts.Token).Forget();
        RunAttackLoop(skillCts.Token).Forget();
        if(IsRegeneration)Regeneration(skillCts.Token).Forget();
    }
    protected virtual void OnDisable()
    {
        _cloak.Reset();
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
    private const float RegenPerSecond = 0.005f; // 초당 1%
    // 은신 렌더링은 EnemyCloak가 전담. 은신 몹이면 Awake에서 Setup, 매 프레임 Tick으로 굴린다.
    private readonly EnemyCloak _cloak = new();

    protected virtual void Update()
    {
        _move.Tick(!IsDead && !IsStunned, MoveSpeed); // active=사망/스턴 아님 → 스턴 중엔 이동 정지(Idle)
        UpdateExposedAttribute();       // 저지 상태에 따라 Hero가 보는 Attribute를 갱신
        _cloak.Tick(CloakClear);
        StunTick();                     // 스턴 만료를 감지해 Animator bool을 끈다
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
        if (IsCloaking && Board != null && Board.IsBlocked(gameObject))
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
        waveSpawner?.EnemyDieEvent();
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
        if(!firstEnable)
        {
            firstEnable = true;
            sc.AddStat(StatType.HP,data.Health);
            sc.AddStat(StatType.ATK,data.Attack);
            sc.AddStat(StatType.AS,data.AttackSpeed);
            sc.AddStat(StatType.DEF,data.Defense);
            sc.AddStat(StatType.SPD,data.MoveSpeed);
            Type = ParseEnum(data.Type, EnemyType.Melee);
            Class = ParseEnum(data.Class, EnemyClass.Normal); 
            Attribute = ParseAttribute(data.Attribute);
            Dataattribute = Attribute;
        }
        else
        {
            sc.SetBaseValue(StatType.HP,data.Health+(gameManager.DayCount*data.UpHealthScale));
            sc.SetBaseValue(StatType.ATK,data.Attack);
            sc.SetBaseValue(StatType.AS,data.AttackSpeed);
            sc.SetBaseValue(StatType.DEF,data.Defense+(data.UpDefenseScale*(gameManager.DayCount/5)));
            sc.SetBaseValue(StatType.SPD,data.MoveSpeed);
        }
        Range = data.Range;
        Hp = sc[StatType.HP];
        IsDead = false;
        //MoveSpeed = data.MoveSpeed;
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
            if(tile.OccupantObject.GetComponent<Hero>().IsDead) continue;
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
        animator.speed = 1f;
        IsDead = true;
        _move.Stop();
        if (Board != null) Board.RemoveEnemy(gameObject);
        if (skillCts == null) { Despawn(); return; }
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
        TriggerDeathSkills();
        waveSpawner.EnemyDieEvent();   
    }

    
    protected virtual void Despawn()
    {
        if (this != null && gameObject != null) Pool.Despawn(gameObject);
    }

}