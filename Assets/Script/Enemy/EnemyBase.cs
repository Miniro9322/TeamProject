using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

using UnityEngine;
using VContainer;

public abstract class EnemyBase : MonoBehaviour,IDamageAble
{
    [SerializeField] protected string enemyKey;
    [SerializeField] protected List<SkillDataSO> skills = new();

    private float[] skillTimers;
    private bool[] skillRunning;
    private CancellationTokenSource skillCts;
    public float Hp { get; protected set; }
    public float MaxHp { get; protected set; }
    public int Defense { get; protected set; }
    public int AttackPower { get; protected set; }
    public float AttackSpeed { get; protected set; }
    public int Range { get; protected set; }
    public float MoveSpeed { get; protected set; }
    public EnemyType Type { get; protected set; }        // 근거리/원거리
    public EnemyClass Class { get; protected set; }      // 일반/엘리트/보스
    public EnemyAttribute Attribute { get; protected set; }
    // 특성 편의 접근자(비트 검사). 여러 특성을 동시에 가질 수 있다.
    public bool IsCloaking => (Attribute & EnemyAttribute.Cloaking) != 0; // 은신
    public bool IsFly      => (Attribute & EnemyAttribute.Fly)      != 0; // 공중
    public bool IsUnJudged => (Attribute & EnemyAttribute.UnJudged) != 0; // 저지 불가
    public bool IsBerserk => (Attribute & EnemyAttribute.Berserk) != 0; //폭주
    public bool IsDead { get; protected set; }
    private StatContainer sc = new();
    public StatContainer SC => sc;
    public Animator animator;
    private bool _attacking; // 공격 모션 재생 중 — 이 동안 스킬 시전을 막아 애니메이터 충돌 방지
    private EnemyMovement _move; // 경로 추종 이동 — Awake에서 생성, 아래 API는 여기로 위임

    // 풀 반환용. 스코프에 PoolManager가 등록되면 주입되고, 아니면 Pool 프로퍼티가 Instance로 폴백.
    private PoolManager _pool;
    private GameManager gameManager;
    [Inject] 
    public void Construct(PoolManager pool,WaveSpawner waveSpawner,GameManager gameManager)
    {
        _pool = pool;
        this.waveSpawner = waveSpawner;
        this.gameManager = gameManager;
    }
    protected PoolManager Pool => _pool ??= PoolManager.Instance; // 파생 클래스(Bat 등)도 재사용
    
    private WaveSpawner waveSpawner;

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

    // 이동 로직은 EnemyMovement가 담당. 스킬·스포너가 쓰는 공개 API는 여기서 그대로 위임한다.
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
    private float _baseMoveSpeed;   // 광폭화 원복용 기본값(ApplyData에서 스냅샷)
    private int _baseAttackPower;
    private Vector3 _baseScale;     // 원래 스케일 — 풀 재사용 시 여기로 복구(분열체가 줄여놓은 걸 리셋)
    public bool IsShielded => Time.time < _shieldExpiry;
    protected virtual void Awake()
    {
        _baseScale = transform.localScale; // 프리팹 원래 스케일 스냅샷(분열 축소 후 복구 기준)
        LoadStats();
        animator = GetComponent<Animator>();
        _move = new EnemyMovement(gameObject, animator, arriveSqr);
        LoadStatContainer();
    }
    protected virtual void OnEnable()
    {
        // Hp = MaxHp;
        // IsDie = false;
        // MoveSpeed = _baseMoveSpeed;
        // AttackPower = _baseAttackPower;
        LoadStats();
        LoadStatContainer();
        _attacking = false; // 죽은 시점 상태가 남아 다음 스폰의 공격/스킬을 막지 않게
        _shieldExpiry = 0f; // 재사용된 적에 이전 쉴드가 남지 않게 초기화
        _berserkOn = false;              // 풀링 재사용 시 광폭화 상태/버프 원복
        SplitGeneration = 0;             // 일반 스폰은 원본(0). 분열체는 스폰 후 SetSplitGeneration으로 덮어씀
        transform.localScale = _baseScale; // 분열체가 줄여놨어도 풀 재사용 시 원래 크기로 복구
        // 애니메이터는 SetActive로 리셋되지 않아 Die 상태에 얼어붙은 채 재사용됨.
        // Rebind로 트리거·파라미터·스테이트를 기본값으로 되돌리고 Update(0)로 즉시 반영.
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
        
        _move.Resume();
        EnemyRegistry.Register(this);
        skillCts = new CancellationTokenSource();
        _move.ArrivedAtCore += HandleArrivedAtCore;
        RunSkillLoop(skillCts.Token).Forget();
        RunAttackLoop(skillCts.Token).Forget();
    }

    protected virtual void OnDisable()
    {
        EnemyRegistry.Unregister(this);
        _move.Pause();
        _move.LeaveBoard(); // 어떤 경로로 사라지든 현재 칸에서 빠진다
        _move.ArrivedAtCore -= HandleArrivedAtCore;
        skillCts?.Cancel();
        skillCts?.Dispose();
        skillCts = null;
    }

    public void EnterMap(MapBoard board, IReadOnlyList<Vector3> waypoints = null, bool snapToStart = true)
    {
        _move.Flying = IsFly; // 공중 특성이면 지형 무시(본진으로 직선). Map/길찾기는 건드리지 않음
        _move.EnterMap(board, waypoints, snapToStart, MoveSpeed, enemyKey);
    }

    public void LoadStatContainer()
    {
        sc.AddStat(StatType.HP,Hp);
        sc.AddStat(StatType.ATK,AttackPower);
        sc.AddStat(StatType.DEF,Defense);
        sc.AddStat(StatType.AS,AttackSpeed);
        sc.AddStat(StatType.SPD,MoveSpeed);
    }
    protected virtual void Update()
    {
        _move.Tick(!IsDead, MoveSpeed); // active=사망 아님 → 원본 게이트(!IsDie)와 동일
    }

    // 본진 도달 시 EnemyMovement가 이벤트로 호출. 도달 후 처리·디스폰은 본체가 쥔다.
    private void HandleArrivedAtCore()
    {
        OnArrivedAtCore();
        if (this != null && gameObject != null) Pool.Despawn(gameObject);
    }

    protected virtual void OnArrivedAtCore()
    {
        // waveSpawner.EnemyDieEvent();
        Board.RemoveEnemy(gameObject);
    }
    private async UniTask RunSkillLoop(CancellationToken token)
    {
        if (skills == null || skills.Count == 0) return;
        skillTimers = new float[skills.Count];
        skillRunning = new bool[skills.Count];

        while (!IsDead)
        {
            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i] == null || skillRunning[i] || skills[i].TriggerOnDeath) continue; // 온데스 스킬은 Die()에서만 발동
                skillTimers[i] += Time.deltaTime;
                // 공격 모션 중이면 시전 보류(타이머는 계속 쌓여서 공격 끝나면 바로 발동).
                if (skillTimers[i] >= skills[i].cooldown && !_attacking)
                {
                    RunSkill(i, token).Forget();
                }
            }
            await UniTask.Yield(token);
        } 
        
    }

    private async UniTask RunSkill(int index, CancellationToken token)
    {
        skillRunning[index] = true;
        try { await skills[index].Execute(this, token); }
        catch (System.OperationCanceledException) { /* 비활성/파괴로 취소 — 정상 */ }
        finally 
        {
            skillRunning[index] = false; 
            skillTimers[index] = 0f;
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
            if (!_attacking && !AnySkillRunning())
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
        AttackPower = data.Attack;
        AttackSpeed = data.AttackSpeed;
        Range = data.Range;
        Defense = data.Defense+(data.UpDefenseScale*(gameManager.DayCount/5));
        MaxHp = data.Health+(gameManager.DayCount*data.UpHealthScale);
        Hp = MaxHp;
        MoveSpeed = data.MoveSpeed;
        _baseMoveSpeed = MoveSpeed;      
        _baseAttackPower = AttackPower;
        Type = ParseEnum(data.Type, EnemyType.Melee);     
        Class = ParseEnum(data.Class, EnemyClass.Normal); 
        Attribute = ParseAttribute(data.Attribute);
        IsDead = false;
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
        int reduce = Defense + (IsShielded ? Mathf.RoundToInt(_damageBlock) : 0);
        int hitDamage = Mathf.Max(1, damage - reduce);                         
        Hp -= hitDamage;
        if (!_berserkOn && Hp < MaxHp * 0.5f && IsBerserk)
        {
            _berserkOn = true;
            MoveSpeed += 3f;
            AttackPower += 10;
        }
        if(Hp<=0)Die();
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;
        Hp = Mathf.Min(Hp + amount, MaxHp);
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
        if(!Board.IsBlocked(gameObject)&&Type==EnemyType.Melee)return;

        transform.LookAt(target.transform);
        _attacking = true; 
        _move.Pause();     
        if (animator != null) animator.SetTrigger("Attack");
        AttackWatchdog(skillCts.Token).Forget(); // 애니 끝나면 상태 복구(이벤트 누락 대비 타임아웃 포함)
    }

    
    public virtual void AnimEvent_AttackHit()
    {
        if (IsDead) return;
        GameObject target = FindAttackTarget(); 
        if (target != null && target.GetComponentInParent<IDamageAble>() is IDamageAble dmg)
        {
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
        IsDead = true;                 // 스킬/공격/이동 루프가 !IsDie 조건으로 스스로 멈춘다
        TriggerDeathSkills();          // 분열 등 온데스 스킬 — 이동 정지/보드 제거 전이라 위치·경로가 유효
        _move.Stop();
        if (Board != null) Board.RemoveEnemy(gameObject); // 죽는 즉시 칸에서 빠져 저지·타겟 대상서 제외
        // waveSpawner.EnemyDieEvent();                 // 이동 정지 + Suspended 해제

        // skillCts가 없으면(이미 비활성) 연출 없이 바로 디스폰.
        if (skillCts == null) { Despawn(); return; }
        DieRoutine(skillCts.Token).Forget();
    }

    // 죽는 순간 TriggerOnDeath 스킬(분열 등)을 발동. 분열은 동기적으로 스폰하므로 즉시 완료된다.
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
        catch (OperationCanceledException) { return; } // 풀 반환/파괴로 취소 — 디스폰 재호출 금지
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

    // 디스폰 지점. 지금은 파괴. 오브젝트 풀링 도입 시 이 메서드만 override해서 pool.Release(this)로 교체.
    protected virtual void Despawn()
    {
        if (this != null && gameObject != null) Pool.Despawn(gameObject);
    }

}


