using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public enum EnemyType
{
    Normal,
    Elite,
    Boss,
}
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
    public EnemyType Type { get; protected set; }
    public bool IsDie { get; protected set; }
    public Animator animator;
    private StatContainer sc;
    private float currentMovespeed;
    private bool isAttack;
    private bool _attacking; // 공격 모션 재생 중 — 이 동안 스킬 시전을 막아 애니메이터 충돌 방지
    private bool _animMoving; // Animator에 보고한 마지막 이동 상태 — 바뀐 프레임에만 SetBool 호출

    // 스킬 하나라도 시전 중인가 — 시전 중엔 공격을 막아 애니메이터를 서로 뺏지 않게 한다.
    private bool AnySkillRunning()
    {
        if (skillRunning == null) return false;
        for (int i = 0; i < skillRunning.Length; i++)
            if (skillRunning[i]) return true;
        return false;
    }

    [Header("Movement")]
    [Tooltip("타일 윗면에서 유닛 피벗을 띄울 높이(월드). 유닛 크기에 맞게 조정.")]
    [SerializeField] private float heightOffset = 0.5f;
    [Tooltip("웨이포인트 도달 판정 거리의 제곱(작을수록 정확). 기본값 유지 권장.")]
    [SerializeField] private float arriveSqr = 0.0004f;

    // 이동 상태 — 스폰→본진 경로(웨이포인트)를 순서대로 따라간다.
    public MapBoard Board { get; private set; }
    private readonly List<Vector3> _path = new();
    private int _pathIndex;
    private Vector2Int _lastCell = new(int.MinValue, int.MinValue); // 직전 칸 — 바뀐 프레임에만 보드 갱신
    private bool _moving;

    public bool HasPath => _path.Count > 0;
    public IReadOnlyList<Vector3> Path => _path; // 대시 등 경로 기준 스킬이 참조
    public int PathIndex => _pathIndex;

    /// <summary>대시/넉백 등으로 앞선 지점에 착지한 뒤, 정상 이동이 그 지점부터 이어지게 목표 인덱스를 맞춘다.</summary>
    public void ResumeFrom(int index) => _pathIndex = Mathf.Clamp(index, 0, _path.Count - 1);

    /// <summary>대시가 도중에 멈춘 경우 등, 현재 위치에서 가장 가까운 경로 지점부터 정상 이동을 이어간다.</summary>
    public void ResumeFromNearest()
    {
        if (_path.Count > 0) _pathIndex = NearestPathIndex(transform.position);
    }

    /// <summary>대시처럼 스킬이 직접 위치를 옮기는 동안 true — 일반 경로 이동이 위치를 덮어쓰지 않게 멈춘다.</summary>
    public bool MovementSuspended { get; set; }

    /// <summary>현재 위치에서 경로를 따라 dist(월드거리)만큼 앞선 지점. 경로 끝이면 마지막 점으로 클램프. landIndex=착지 세그먼트.</summary>
    public Vector3 PointAhead(float dist, out int landIndex)
    {
        if (_path.Count == 0) { landIndex = -1; return transform.position; }

        Vector3 pos = transform.position;
        for (int i = _pathIndex; i < _path.Count; i++)
        {
            Vector3 seg = _path[i] - pos;
            float len = seg.magnitude;
            if (len >= dist) { landIndex = i; return pos + seg.normalized * dist; }
            dist -= len;
            pos = _path[i];
        }
        landIndex = _path.Count - 1; // 본진 도달 — 오버슈트 방지
        return _path[^1];
    }

    protected virtual void Awake()
    {
        LoadStats();
        animator = GetComponent<Animator>();
    }

    protected virtual void OnEnable()
    {
        IsDie =false;
        EnemyRegistry.Register(this);
        skillCts = new CancellationTokenSource();
        RunSkillLoop(skillCts.Token).Forget();
        RunAttackLoop(skillCts.Token).Forget();
    }

    protected virtual void OnDisable()
    {
        EnemyRegistry.Unregister(this);
        _moving = false;
        if (Board != null) Board.RemoveEnemy(gameObject); // 어떤 경로로 사라지든 현재 칸에서 빠진다
        skillCts?.Cancel();
        skillCts?.Dispose();
        skillCts = null;
    }

    // ---- 이동 (스폰→본진 경로 추종) ----
    public void EnterMap(MapBoard board, IReadOnlyList<Vector3> waypoints = null, bool snapToStart = true)
    {
        Board = board;
        _path.Clear();
        _pathIndex = 0;
        _moving = false;

        if (board == null)
        {
            Debug.LogWarning($"[{name}] MapBoard가 주입되지 않아 이동할 수 없습니다.", this);
            return;
        }

        // 웨이포인트는 타일 윗면 기준(offset 0)으로 받아, 유닛별 높이(heightOffset)만 여기서 더한다.
        IReadOnlyList<Vector3> src = waypoints ?? board.GetWaypoints(0f);
        foreach (Vector3 p in src) _path.Add(p  ); //+ Vector3.up* heightOffset

        if (_path.Count == 0)
        {
            Debug.LogWarning($"[{name}] 스폰→본진 경로가 없습니다. (스폰/본진 배치·통행 지형 확인)", this);
            return;
        }

        // MoveSpeed 기본값 0 = 제자리(이동 버그처럼 보이지만 대개 EnemyTable 행/로드 누락). 자가진단.
        if (MoveSpeed <= 0f)
            Debug.LogWarning($"[{name}] MoveSpeed={MoveSpeed} — 제자리에 멈춥니다. EnemyTable '{enemyKey}' 행 확인.", this);

        _moving = true;
        if (snapToStart)
            transform.position = _path[0]; // 스폰 지점에서 시작
        else
            _pathIndex = NearestPathIndex(transform.position); // 현재 위치 유지, 가까운 지점부터 이어감

        _lastCell = board.WorldToCell(transform.position);
        board.MoveEnemy(gameObject, transform.position); // 현재 칸 등록
    }

    // 경로 중 현재 위치에서 가장 가까운 웨이포인트 인덱스(소환·중간 투입 시 시작점).
    private int NearestPathIndex(Vector3 pos)
    {
        int best = 0;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < _path.Count; i++)
        {
            float d = (_path[i] - pos).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = i; }
        }
        return best;
    }

    protected virtual void Update()
    {
        MoveAlongPath();
    }

    private void MoveAlongPath()
    {
        if (!_moving || IsDie || Board == null || MovementSuspended) { SetMoving(false); return; }

        // 근접 영웅에게 저지당하면 그 자리에서 정지(타일 저지 시스템). 풀리면 다시 전진.
        bool advancing = !Board.IsBlocked(gameObject);
        if (advancing)
        {
            Vector3 target = _path[_pathIndex];
            transform.position = Vector3.MoveTowards(transform.position, target, MoveSpeed * Time.deltaTime);
            FaceToward(target);
        }
        SetMoving(advancing); // 저지되면 Idle, 풀리면 Move — 상태 바뀐 프레임에만 반영

        // 현재 칸이 바뀐 프레임에만 보드에 보고 → 저지·커버·타겟팅이 이걸로 갱신된다.
        Vector2Int now = Board.WorldToCell(transform.position);
        if (now != _lastCell)
        {
            _lastCell = now;
            Board.MoveEnemy(gameObject, transform.position);
        }

        // 이번 웨이포인트 도달 → 다음 목표로.
        if ((transform.position - _path[_pathIndex]).sqrMagnitude > arriveSqr) return;
        _pathIndex++;
        if (_pathIndex >= _path.Count) ArriveAtCore();
    }

    // 이동 상태를 Animator에 반영 — 바뀐 프레임에만 SetBool을 호출해 낭비/리셋 방지.
    private void SetMoving(bool moving)
    {
        if (_animMoving == moving) return;
        _animMoving = moving;
        if (animator != null) animator.SetBool("IsMoving", moving);
    }

    private void FaceToward(Vector3 target)
    {
        Vector3 flat = target - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(flat), 12f * Time.deltaTime);
    }

    protected void ArriveAtCore()
    {
        _moving = false;
        OnArrivedAtCore();
        if (this != null && gameObject != null) Destroy(gameObject);
    }

    protected virtual void OnArrivedAtCore() { }
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
                    skillTimers[i] = 0f;
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
        finally { skillRunning[index] = false; }
    }

    private async UniTask RunAttackLoop(CancellationToken token)
    {
        float attackTimer = 0f;
        while (!IsDie)
        {
            float interval = AttackSpeed > 0f ? 1f / AttackSpeed : 1f; //attackspped = 초당 공격횟수
            attackTimer += Time.deltaTime;
            // 스킬 시전 중이거나 이미 공격 모션 중이면 공격 보류(타이머 유지 → 풀리면 바로 공격).
            if (attackTimer >= interval && !_attacking && !AnySkillRunning())
            {
                attackTimer = 0f;
                Attack();
            }
            await UniTask.Yield(token);
        }
    }

    protected void LoadStats()
    {
        if (string.IsNullOrEmpty(enemyKey)) enemyKey = GetType().Name;

        var enemyTable = DataTableManager.Get<EnemyTable>(DataTableIds.Enemy);
        if (enemyTable == null) return;

        var data = enemyTable.Get(enemyKey);
        if (data == null)
        {
            Debug.LogWarning($"EnemyBase: EnemyTable에서 '{enemyKey}' 데이터를 찾을 수 없음");
            return;
        }
        ApplyData(data);
        LoadSkills(data.Skills);
    }

    private void LoadSkills(string skillIds)
    {
        if (string.IsNullOrEmpty(skillIds)) return;

        foreach (var raw in skillIds.Split(';'))
        {
            var id = raw.Trim();
            if (string.IsNullOrEmpty(id)) continue;

            var so = Resources.Load<SkillDataSO>($"Skills/{id}");
            if (so == null)
            {
                Debug.LogWarning($"EnemyBase: 스킬 '{id}' 로드 실패 (Resources/Skills/{id}). 임포터를 먼저 실행했는지 확인");
                continue;
            }
            if (!skills.Contains(so)) skills.Add(so);
        }
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
        Type = ParseType(data.Type);
        IsDie = false;
    }

    private static EnemyType ParseType(string raw)
    {
        if (!string.IsNullOrEmpty(raw) && System.Enum.TryParse(raw.Trim(), true, out EnemyType type))
            return type;
        return EnemyType.Normal;
    }

    public void TakeDamage(int damage)
    {
        if(IsDie)return;
        int hitDamage = Mathf.Max(1,damage-Defense);
        Hp -= hitDamage;
        if(Hp<=0)Die();
    }

    // 힐 (MaxHp 초과 안 함)
    public void Heal(float amount)
    {
        if (IsDie || amount <= 0f) return;
        Hp = Mathf.Min(Hp + amount, MaxHp);
    }

    // 기본공격: 현재 칸 기준 사거리(Range 칸) 안 타일에 배치된 영웅(점유자) 중 가장 가까운 대상을 때림.
    // HeroRegistry가 아니라 보드 타일에 저장된 OccupantObject를 조회한다. 없으면 아무 일도 안 함.
    public virtual void Attack()
    {
        if (IsDie || Board == null || skillCts == null) return;
        if (FindAttackTarget() == null) return; // 사거리에 대상 없으면 멈추지도, 공격하지도 않음

        _attacking = true; // 동기적으로 세팅 → 스킬 루프가 곧바로 공격 중임을 인지
        _moving = false;   // 공격 동안 정지
        if (animator != null) animator.SetTrigger("Attack");
        AttackWatchdog(skillCts.Token).Forget(); // 애니 끝나면 상태 복구(이벤트 누락 대비 타임아웃 포함)
    }

    /// <summary>타격 프레임 애니메이션 이벤트가 호출 → 이 순간 사거리 대상에게 데미지.</summary>
    public void AnimEvent_AttackHit()
    {
        if (IsDie) return;
        GameObject target = FindAttackTarget(); 
        if (target != null && target.GetComponentInParent<IDamageAble>() is IDamageAble dmg)
            dmg.TakeDamage(AttackPower);
    }

    // 현재 칸 기준 사거리(Range 칸) 안 타일 점유자 중 가장 가까운 대상.
    private GameObject FindAttackTarget()
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

    // 공격 애니메이션이 끝나면 _attacking/_moving 복구. 이벤트를 놓쳐도 적이 멈추지 않게 하는 안전장치.
    private async UniTask AttackWatchdog(CancellationToken token)
    {
        try { await WaitForAttackAnim("Attack", 5f, token); }
        catch (OperationCanceledException) { }
        finally
        {
            _attacking = false; // 공격 모션 끝 → 스킬/다음 공격 허용
            _moving = true;
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
        _moving = false;
        MovementSuspended = false;
        if (Board != null) Board.RemoveEnemy(gameObject); // 죽는 즉시 칸에서 빠져 저지·타겟 대상서 제외

        // skillCts가 없으면(이미 비활성) 연출 없이 바로 디스폰.
        if (skillCts == null) { Despawn(); return; }
        DieRoutine(skillCts.Token).Forget();
    }

    // Die 애니메이션을 끝까지 재생한 뒤 디스폰. 토큰은 활성 수명(OnDisable에서 Cancel)에 묶는다.
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

    // 죽음 연출 대기: Summon용과 달리 IsDie로 중단하지 않는다(죽는 중이 정상 상태).
    // 스테이트를 못 찾으면 timeout 후 그냥 진행해 무한 대기를 막는다.
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
        if (this != null && gameObject != null) Destroy(gameObject);
    }

    // Ai한테 부탁한 예시
    // ===================== 예시용 (실제 로직 아님) =====================
    // 고른 방식: [공유 HP 풀] + [오라(동적)] 보호막
    //  - 공유 HP : 보호막 HP가 하나. 범위 안 누가 맞든 이 HP가 깎이고, 0되면 전원 해제
    //  - 오라     : 맞는 그 순간 내가 보호막 반경 안이면 보호. 들어오면 즉시 보호/나가면 해제

    // 공유 보호막 한 덩어리. 엘리트가 시전하면 하나 만들어져서 모두가 참조만 함.
    public class ShieldInstance
    {
        public float Hp;              // 공유 HP (한 밑바)
        public Vector3 Center;        // 중심(=엘리트 위치). 오라라 계속 갱신 가능
        public float Radius;          // 보호 반경
        private readonly float expireTime;

        public ShieldInstance(float hp, Vector3 center, float radius, float duration)
        {
            Hp = hp;
            Center = center;
            Radius = radius;
            expireTime = Time.time + duration;
        }

        // HP 남아있고 시간 안 지났으면 살아있음
        public bool IsActive => Hp > 0f && Time.time < expireTime;

        // 오라 판정: 이 위치가 지금 반경 안인가?
        public bool Covers(Vector3 pos) => (pos - Center).sqrMagnitude <= Radius * Radius;

        public void Absorb(float dmg) => Hp -= dmg;
    }

    // 예시: 엘리트가 시전한 공유 보호막. 오라라서 위치만 맞으면 아무나 보호받음.
    // (실제로는 시전자나 매니저가 들고 있게 됨 — 여긴 데모라 static)
    public static ShieldInstance ActiveShield;

    // 예시용 데미지 처리. 실제 TakeDamage와 별개로, 위 두 방식이 어떻게 도는지 보여줌.
    public void TestTakeDamage(int damage)
    {
        if (IsDie) return;
        int hitDamage = Mathf.Max(1, damage - Defense);

        // [오라] 맞는 순간 내가 반경 안 && 막이 살아있으면 → [공유풀]이 대신 받음
        if (ActiveShield != null && ActiveShield.IsActive && ActiveShield.Covers(transform.position))
        {
            ActiveShield.Absorb(hitDamage);
            Debug.Log($"[Shield] {name} 이(가) 피해 {hitDamage} → 공유막이 흡수, 공유막 HP {ActiveShield.Hp}");
            return;   // 내 HP는 안 깎임
        }

        // 보호 못 받으면 평소대로 내 HP가 깎임
        Hp -= hitDamage;
        Debug.Log($"[Normal] {name} 이(가) 피해 {hitDamage} → 내 HP {Hp}");
        if (Hp <= 0) Die();
    }
    // ================================================================

}


