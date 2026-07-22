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
    [Tooltip("은신(Cloaking) 공용 설정. IsCloaking일 때만 사용 — 걸을 땐 은신 재질, 저지 시 원래 재질.")]
    [SerializeField] private CloakSettingsSO cloakSettings;

    private float[] skillTimers;
    private bool[] skillRunning;
    private CancellationTokenSource skillCts;
    [field: SerializeField] public float Hp { get; protected set; }   // 인스펙터 표시용(런타임 값 확인). 값은 ApplyData/재생/피격이 갱신.
    [field: SerializeField] public float MaxHp { get; protected set; }
    public int Defense { get; protected set; }
    public int AttackPower { get; protected set; }
    public float AttackSpeed { get; protected set; }
    public int Range { get; protected set; }
    public float MoveSpeed { get; protected set; }
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
    public StatContainer SC => sc;
    public Animator animator;
    private bool _attacking; // 공격 모션 재생 중 — 이 동안 스킬 시전을 막아 애니메이터 충돌 방지
    private EnemyMovement _move; // 경로 추종 이동 — Awake에서 생성, 아래 API는 여기로 위임
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
    protected virtual void Awake()
    {
        _baseScale = transform.localScale; // 프리팹 원래 스케일 스냅샷(분열 축소 후 복구 기준)
        LoadStats();
        animator = GetComponent<Animator>();
        _move = new EnemyMovement(gameObject, animator, arriveSqr);
        LoadStatContainer();
        SetupCloak();
    }

    protected virtual void OnEnable()
    {
        LoadStats();
        LoadStatContainer();
        _attacking = false;
        _shieldExpiry = 0f; 
        _berserkOn = false;             
        SplitGeneration = 0;           
        transform.localScale = _baseScale; 
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
        
        _move.Resume();
        ResetCloak();
        EnemyRegistry.Register(this);
        skillCts = new CancellationTokenSource();
        _move.ArrivedAtCore += HandleArrivedAtCore;
        RunSkillLoop(skillCts.Token).Forget();
        RunAttackLoop(skillCts.Token).Forget();
        if(IsRegeneration)Regeneration(skillCts.Token).Forget();
    }
    protected virtual void OnDisable()
    {
        EnemyRegistry.Unregister(this);
        ResetCloak();
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
    // 재생 특성 회복량(초당 최대체력 비율). 매 프레임 deltaTime만큼 나눠 채워 부드럽게 차오른다.
    private const float RegenPerSecond = 0.01f; // 초당 1%
    // ── 은신(Cloaking) 렌더링 ────────────────────────────────
    // 재질 교체 없이, 은신 셰이더의 _CloakAmount를 0~1로 보간해 [본체 ↔ 흐릿]을 "점점" 전환한다.
    private static readonly int CloakAmountId = Shader.PropertyToID("_CloakAmount");
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int LegacyColorId = Shader.PropertyToID("_Color");
    private Renderer[] _cloakRenderers;
    private MaterialPropertyBlock _cloakMpb;
    private Material[][] _originalMats;   // 렌더러별 원래 재질 — 드러날 때(amount=0) 이걸로 복귀(원본 조명/색 그대로)
    private Material[][] _cloakMats;      // 렌더러별 은신 재질 — 은신/전환 중에만 사용
    private bool _cloakApplied;           // 현재 은신 재질이 올라가 있는지
    private float _cloakAmount;          // 0=또렷 ~ 1=은신. 매 프레임 목표값으로 보간.

    protected virtual void Update()
    {
        _move.Tick(!IsDead, MoveSpeed); // active=사망 아님 → 원본 게이트(!IsDie)와 동일
        UpdateExposedAttribute();       // 저지 상태에 따라 Hero가 보는 Attribute를 갱신
        CloakTick();
    }

    // Hero가 읽는 공개 Attribute 갱신.
    // 은신 유닛이 저지당하는 동안엔 Cloaking 비트를 빼서 Hero의 unattackable 필터를 통과(=공격 가능)시킨다.
    // 저지가 풀리면 다시 원본으로 돌아가 공격 불가. 연출(CloakTick)과 동일한 IsBlocked 조건이라 "보이는 것=때릴 수 있는 것"이 항상 일치.
    private void UpdateExposedAttribute()
    {
        EnemyAttribute exposed = Dataattribute;
        if (IsCloaking && Board != null && Board.IsBlocked(gameObject))
            exposed &= ~EnemyAttribute.Cloaking;
        Attribute = exposed;
    }

    // 걸을 때(저지 안 됨) → 은신(1)로 페이드, 저지/사망 시 → 또렷(0)로 페이드.
    private void CloakTick()
    {
        if (!IsCloaking || _cloakRenderers == null) return;

        bool clear = IsDead || (Board != null && Board.IsBlocked(gameObject)); // 저지/사망이면 또렷
        float target = clear ? 0f : 1f;
        float next = Mathf.MoveTowards(_cloakAmount, target, cloakSettings.fadeSpeed * Time.deltaTime);
        if (next == _cloakAmount) return;
        _cloakAmount = next;

        bool needCloak = _cloakAmount > 0.0001f;
        ApplyCloakMaterial(needCloak);          // amount>0 → 은신 재질 / amount==0 → 원래 재질(원본 그대로)
        if (needCloak) SetCloakAmount(_cloakAmount);
    }

    // 은신 재질 ↔ 원래 재질 전환. 원래 재질을 영구히 덮지 않고, 필요할 때만 갈아끼운다.
    private void ApplyCloakMaterial(bool on)
    {
        if (on == _cloakApplied) return;
        for (int i = 0; i < _cloakRenderers.Length; i++)
            _cloakRenderers[i].sharedMaterials = on ? _cloakMats[i] : _originalMats[i];
        _cloakApplied = on;
    }

    // 모든 렌더러의 _CloakAmount를 MPB로 갱신(재질 인스턴스 생성 없이 개체별로).
    private void SetCloakAmount(float amount)
    {
        for (int i = 0; i < _cloakRenderers.Length; i++)
        {
            _cloakRenderers[i].GetPropertyBlock(_cloakMpb);
            _cloakMpb.SetFloat(CloakAmountId, amount);
            _cloakRenderers[i].SetPropertyBlock(_cloakMpb);
        }
    }

    // 은신 몬스터일 때만: 렌더러를 은신 재질로 두고, 본체 텍스처를 MPB로 주입(재질 하나 공유 가능).
    // Awake에서 LoadStats 뒤에 호출(Attribute 결정된 뒤).
    private const string CloakSettingsPath = "Skills/CloakSettings"; // Resources/CloakSettings.asset
    private void SetupCloak()
    {
        if (!IsCloaking) return;
        if (cloakSettings == null) cloakSettings = Resources.Load<CloakSettingsSO>(CloakSettingsPath);
        if (cloakSettings == null || cloakSettings.cloakMaterial == null) return;

        _cloakRenderers = GetComponentsInChildren<Renderer>(true);
        _cloakMpb = new MaterialPropertyBlock();
        _originalMats = new Material[_cloakRenderers.Length][];
        _cloakMats = new Material[_cloakRenderers.Length][];
        for (int i = 0; i < _cloakRenderers.Length; i++)
        {
            Renderer r = _cloakRenderers[i];
            Material src = r.sharedMaterial;                            // 원래 재질(안 덮어씀, 드러날 때 복귀용)
            Texture baseTex = src != null ? src.mainTexture : null;     // 본체 텍스처(없으면 흰색 폴백)
            Color baseColor = GetBaseColor(src);                        // 본체 색(_BaseColor/_Color)

            Material[] orig = r.sharedMaterials;
            _originalMats[i] = orig;                                    // 원래 재질 배열 보관
            Material[] cloak = new Material[orig.Length];
            for (int j = 0; j < cloak.Length; j++) cloak[j] = cloakSettings.cloakMaterial;
            _cloakMats[i] = cloak;

            // 은신 재질이 올라갔을 때 원래 텍스처·색을 재현하도록 MPB에 미리 넣어둠(전환 중 본체가 섞여 보임).
            r.GetPropertyBlock(_cloakMpb);
            if (baseTex != null) _cloakMpb.SetTexture(BaseMapId, baseTex);
            _cloakMpb.SetColor(BaseColorId, baseColor);
            _cloakMpb.SetFloat(CloakAmountId, 0f);
            r.SetPropertyBlock(_cloakMpb);
        }
        _cloakApplied = false; // 시작은 원래 재질 그대로(또렷)
    }

    // 원래 재질의 본체 색을 최대한 정확히 뽑아온다(_BaseColor 우선, 없으면 _Color, 둘 다 없으면 흰색).
    private static Color GetBaseColor(Material src)
    {
        if (src == null) return Color.white;
        if (src.HasProperty(BaseColorId)) return src.GetColor(BaseColorId);
        if (src.HasProperty(LegacyColorId)) return src.GetColor(LegacyColorId);
        return Color.white;
    }

    // 풀 재사용 대비: 은신량 초기화 + 원래 재질로 복귀(또렷 상태로).
    private void ResetCloak()
    {
        _cloakAmount = 0f;
        if (_cloakRenderers != null) ApplyCloakMaterial(false);
    }



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
            gm.HpDamage();
        if (Board != null) Board.RemoveEnemy(gameObject);
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
        if(gameManager ==null)
        {
            Defense = data.Defense;
            MaxHp = data.Health;
        }
        else
        {
            Defense = data.Defense+(data.UpDefenseScale*(gameManager.DayCount/5));
            MaxHp = data.Health+(gameManager.DayCount*data.UpHealthScale);
        }
        Hp = MaxHp;
        MoveSpeed = data.MoveSpeed;
        Type = ParseEnum(data.Type, EnemyType.Melee);     
        Class = ParseEnum(data.Class, EnemyClass.Normal); 
        Attribute = ParseAttribute(data.Attribute);
        Dataattribute = Attribute;
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
        IsDead = true;
        _move.Stop();
        if (Board != null) Board.RemoveEnemy(gameObject); // 죽는 즉시 칸에서 빠져 저지·타겟 대상서 제외

        Debug.Log("사망 플래그 발동");
        waveSpawner.EnemyDieEvent();                 // 이동 정지 + Suspended 해제

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
        TriggerDeathSkills();
    }

    // 디스폰 지점. 지금은 파괴. 오브젝트 풀링 도입 시 이 메서드만 override해서 pool.Release(this)로 교체.
    protected virtual void Despawn()
    {
        if (this != null && gameObject != null) Pool.Despawn(gameObject);
    }

}