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
    public bool IsDie { get; protected set; }
    private StatContainer sc = new();
    public StatContainer SC => sc;
    public Animator animator;
    private bool _attacking; // 공격 모션 재생 중 — 이 동안 스킬 시전을 막아 애니메이터 충돌 방지
    private EnemyMovement _move; // 경로 추종 이동 — Awake에서 생성, 아래 API는 여기로 위임

    // 풀 반환용. 스코프에 PoolManager가 등록되면 주입되고, 아니면 Pool 프로퍼티가 Instance로 폴백.
    private PoolManager _pool;
    [Inject] 
    public void Construct(PoolManager pool,WaveSpawner waveSpawner)
    {
        _pool = pool;
        this.waveSpawner = waveSpawner;
    }
    protected PoolManager Pool => _pool ??= PoolManager.Instance; // 파생 클래스(Bat 등)도 재사용
    private WaveSpawner waveSpawner;

    private bool AnySkillRunning()
    {   
        if (skillRunning == null) return false;
        for (int i = 0; i < skillRunning.Length; i++)
            if (skillRunning[i]) return true;
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

    protected virtual void Awake()
    {
        LoadStats();
        animator = GetComponent<Animator>();
        _move = new EnemyMovement(gameObject, animator, arriveSqr);
        
        LoadStatContainer();
    }
    protected virtual void OnEnable()
    {
        // 풀 재사용 대비: Awake는 1회뿐이라 스폰마다 런타임 상태를 여기서 되돌린다.
        Hp = MaxHp;        // 스탯 로드는 Awake에서만 → HP는 스폰마다 복구
        IsDie = false;
        _attacking = false; // 죽은 시점 상태가 남아 다음 스폰의 공격/스킬을 막지 않게
        _shieldExpiry = 0f; // 재사용된 적에 이전 쉴드가 남지 않게 초기화

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
        => _move.EnterMap(board, waypoints, snapToStart, MoveSpeed, enemyKey);

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
        _move.Tick(!IsDie, MoveSpeed); // active=사망 아님 → 원본 게이트(!IsDie)와 동일
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

        while (!IsDie)
        {
            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i] == null || skillRunning[i]) continue;
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
        while (!IsDie)
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
        Defense = data.Defense;
        MaxHp = data.Health;
        Hp = data.Health;
        MoveSpeed = data.MoveSpeed;
        Type = ParseEnum(data.Type, EnemyType.Melee);       // 근거리/원거리 (기본 Melee)
        Class = ParseEnum(data.Class, EnemyClass.Normal);   // 등급 (기본 Normal)
        Attribute = ParseAttribute(data.Attribute); //기본 None. '|'/';'로 구분된 여러 특성 조합
        IsDie = false;
    }

    // CSV 문자열 → enum. 비었거나 못 읽으면 fallback으로 대체(대소문자 무시).
    private static T ParseEnum<T>(string raw, T fallback) where T : struct, System.Enum
    {
        if (!string.IsNullOrEmpty(raw) && System.Enum.TryParse(raw.Trim(), true, out T value))
            return value;
        return fallback;
    }

    // CSV 문자열 → 플래그 특성. '|'/';'로 구분된 각 토큰을 OR로 합친다(대소문자 무시).
    // 매칭 안 되는 토큰은 조용히 무시되므로, 특성이 안 먹으면 CSV 철자부터 확인할 것.
    private static EnemyAttribute ParseAttribute(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return EnemyAttribute.None;
        EnemyAttribute result = EnemyAttribute.None;
        foreach (string token in raw.Split(new[] { '|', ';' }, StringSplitOptions.RemoveEmptyEntries))
            if (System.Enum.TryParse(token.Trim(), true, out EnemyAttribute flag))
                result |= flag;
        return result;
    }

    // --- 데미지 경감 쉴드(스콜피온 쉴드 스킬 등이 부여) — 고정 수치만큼 경감(추가 방어력처럼 동작) ---
    private float _damageBlock;   // 고정 감소 수치
    private float _shieldExpiry;  // Time.time 기준 만료 시각 — 코루틴 없이 지연 만료(풀링 안전)
    public bool IsShielded => Time.time < _shieldExpiry;

    /// <summary>고정 수치 데미지 경감 쉴드 부여. 재시전 시 갱신(수치·지속시간 덮어씀).</summary>
    public void ApplyDamageReductionShield(float flatReduce, float duration)
    {
        _damageBlock = Mathf.Max(0f, flatReduce);
        _shieldExpiry = Time.time + duration;
    }

    public void TakeDamage(int damage)
    {
        if(IsDie)return;
        int reduce = Defense + (IsShielded ? Mathf.RoundToInt(_damageBlock) : 0); // 쉴드 = 고정 추가 방어
        int hitDamage = Mathf.Max(1, damage - reduce);                            // 최소 1은 보장
        Hp -= hitDamage;
        if(Hp<=0)Die();
    }

    public void Heal(float amount)
    {
        if (IsDie || amount <= 0f) return;
        Hp = Mathf.Min(Hp + amount, MaxHp);
    }
    public virtual void Attack()
    {
        if (IsDie || Board == null || skillCts == null) return;
        GameObject target = FindAttackTarget();
        if (target == null) return; // 사거리에 대상 없으면 멈추지도, 공격하지도 않음
        if(!Board.IsBlocked(gameObject)&&Type==EnemyType.Melee)return;

        transform.LookAt(target.transform); // 실제 공격 순간에만 대상을 바라본다(타겟 확정 후 → null 안전)
        _attacking = true; // 동기적으로 세팅 → 스킬 루프가 곧바로 공격 중임을 인지
        _move.Pause();     // 공격 동안 정지
        if (animator != null) animator.SetTrigger("Attack");
        AttackWatchdog(skillCts.Token).Forget(); // 애니 끝나면 상태 복구(이벤트 누락 대비 타임아웃 포함)
    }

    
    public virtual void AnimEvent_AttackHit()
    {
        if (IsDie) return;
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
        if (IsDie) return;
        IsDie = true;                 // 스킬/공격/이동 루프가 !IsDie 조건으로 스스로 멈춘다
        _move.Stop();
        if (Board != null) Board.RemoveEnemy(gameObject); // 죽는 즉시 칸에서 빠져 저지·타겟 대상서 제외
        // waveSpawner.EnemyDieEvent();                 // 이동 정지 + Suspended 해제

        // skillCts가 없으면(이미 비활성) 연출 없이 바로 디스폰.
        if (skillCts == null) { Despawn(); return; }
        DieRoutine(skillCts.Token).Forget();
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


