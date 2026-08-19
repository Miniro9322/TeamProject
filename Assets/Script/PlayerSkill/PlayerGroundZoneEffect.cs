using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

// GroundZoneEffect와 같은 골격(자기 완결형 스폰 프리팹)을 쓰되 Hero owner에 의존하지 않는다.
// 플레이어는 ATK 스탯도, 트레잇도, 영웅별 이펙트 풀도 없으므로 데미지/힐은 장판 자신이 들고 있는
// 고정값을 쓰고, 이펙트는 풀링 없이 단순 Instantiate/Destroy로 처리한다. 오라(owner 생존 동안 무한
// 지속)도 지원하지 않는다 — duration은 항상 양수여야 한다.
[DisallowMultipleComponent]
public class PlayerGroundZoneEffect : MonoBehaviour
{
    public GroundZoneMode mode = GroundZoneMode.Damage;
    public RangeShape shape = RangeShape.Diamond;
    public int radius = 1;
    public float tickInterval = 1f;
    [Tooltip("mode==Damage: 틱당 고정 데미지 (플레이어는 ATK 스탯이 없어 고정값 사용)")]
    public float damageAmount = 0f;
    [Tooltip("mode==Heal: 틱당 고정 힐량 (범위 내 최저 체력 아군 1명)")]
    public float healAmount = 0f;
    [Tooltip("mode==Heal: 대상 최대 체력 비례 추가 힐량")]
    public float hpHealPer = 0f;
    [Tooltip("반드시 양수. 플레이어 장판은 오라(무한 지속)를 지원하지 않는다.")]
    public float duration = 3f;
    public List<TargetDebuffRef> targetDebuffs = new();
    public GameObject hitEffect;
    public float hitEffectLifetime = 0.5f;
    [Tooltip("이 프리팹의 파티클/데칼이 기본 크기(localScale=1)로 나타내는 반경(타일 수). radius/이값만큼 자기 자신을 스케일한다. 0이면 스케일하지 않음.")]
    public float visualRadius = 0f;
    [Tooltip("장판이 살아있는 동안 자기 위치에 한 번 스폰해 유지하는 이펙트(범위 표시용). null이면 안 스폰.")]
    public GameObject selfEffect;
    [Tooltip("selfEffect가 기본 크기(localScale=1)로 나타내는 반경(타일 수). radius/이값만큼 스케일한다. 0이면 스케일하지 않음.")]
    public float selfEffectVisualRadius = 0f;
    [Tooltip("mode==Buff: 범위 안 아군에게 적용할 버프 목록. duration/maxStacks는 무시된다 — 범위에 머무는 동안 유지되고 벗어나면 즉시 제거된다.")]
    public List<BuffEffect> allyBuffs = new();

    private MapBoard board;
    private BuffManager buffManager;
    private Action<GameObject> release;
    private CancellationTokenSource cts;
    private GameObject selfEffectInstance;
    private readonly HashSet<Hero> buffedAllies = new();
    private GameManager gameManager;

    // 스폰 직후 호출한다. release가 null이면 만료 시 스스로 Destroy된다(풀링 없음).
    public void Init(MapBoard board, BuffManager buffManager, Action<GameObject> release = null, GameManager gameManager = null)
    {
        this.board = board;
        this.buffManager = buffManager;
        this.release = release;
        this.gameManager = gameManager;
        if (this.gameManager != null) this.gameManager.ChangeToDay += ForceEnd;
        ApplyVisualScale();
        SpawnSelfEffect();
        if (duration > 0f) FitParticlesToDuration(duration);
    }

    private void OnEnable()
    {
        cts = new CancellationTokenSource();
        RunLifetime(cts.Token).Forget();
    }

    private void OnDisable()
    {
        if (gameManager != null) gameManager.ChangeToDay -= ForceEnd;
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
    }

    // 밤이 끝나면(ChangeToDay) 남은 duration과 무관하게 즉시 만료 처리한다.
    private void ForceEnd() => cts?.Cancel();

    private async UniTask RunLifetime(CancellationToken token)
    {
        try
        {
            float elapsed = 0f, tickTimer = 0f;
            while (!token.IsCancellationRequested && elapsed < duration)
            {
                float dt = Time.deltaTime;
                elapsed += dt;
                tickTimer += dt;
                if (tickTimer >= tickInterval) { tickTimer = 0f; Tick(); }
                await UniTask.Yield(token);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (mode == GroundZoneMode.Buff)
                ClearAllyBuffs();
            DespawnSelfEffect();

            if (this != null)
            {
                if (release != null) release.Invoke(gameObject);
                else Destroy(gameObject);
            }
        }
    }

    // 장판이 어떤 경로로 끝나든(자연 만료/ForceEnd 강제 종료) 로직과 시각 이펙트를 같은 타이밍에 정리한다.
    private void DespawnSelfEffect()
    {
        if (selfEffectInstance == null) return;
        Destroy(selfEffectInstance);
        selfEffectInstance = null;
    }

    // Buff 모드 장판이 사라질 때(정상 만료/파괴) 아직 범위 안에 있던 아군의 버프도 정리한다.
    private void ClearAllyBuffs()
    {
        if (buffedAllies.Count == 0) return;
        foreach (Hero ally in buffedAllies)
        {
            if (ally == null) continue;
            foreach (BuffEffect effect in allyBuffs)
                buffManager.RemoveBuff(ally, effect.statType, this);
        }
        buffedAllies.Clear();
    }

    private void Tick()
    {
        if (board == null) return;

        if (mode == GroundZoneMode.Heal)
        {
            if (healAmount <= 0f && hpHealPer <= 0f) return;
            Hero target = AttackDamageUtil.FindLowestHpAlly(QueryAllies());
            if (target == null) return;
            float heal = healAmount + target.SC[StatType.HP] * hpHealPer;
            if (heal <= 0f) return;
            target.Heal(heal);
            SpawnHitEffect(transform.position);
            return;
        }

        if (mode == GroundZoneMode.Buff)
        {
            var inRange = new HashSet<Hero>();
            foreach (GameObject go in QueryAllies())
                if (go.GetComponentInParent<Hero>() is Hero ally)
                    inRange.Add(ally);

            foreach (Hero ally in inRange)
            {
                if (!buffedAllies.Add(ally)) continue;
                foreach (BuffEffect effect in allyBuffs)
                    buffManager.ApplyStackingModifier(ally, effect.statType, effect.modifierType, effect.value, 0f, effect.maxStacks, this);
            }

            buffedAllies.RemoveWhere(ally =>
            {
                if (ally != null && inRange.Contains(ally)) return false;
                if (ally != null)
                    foreach (BuffEffect effect in allyBuffs)
                        buffManager.RemoveBuff(ally, effect.statType, this);
                return true;
            });
            return;
        }

        int dmg = Mathf.RoundToInt(damageAmount);
        // 장판(지상)은 공중 적을 절대 때릴 수 없다 — GroundZoneEffect와 동일한 불변식.
        foreach (GameObject go in QueryEnemies(EnemyAttribute.Fly))
        {
            if (dmg > 0 && go.GetComponentInParent<IDamageAble>() is IDamageAble d)
            {
                d.TakeDamage(dmg);
                SpawnHitEffect(go.transform.position);
            }

            AttackDamageUtil.ApplyTargetDebuffs(go.transform, targetDebuffs, buffManager, this);
        }
    }

    // Hero.GetObjectsInRange(RangeQueryAffinity.Ally)와 동일한 로직 — 타일당 1개 점유자 모델을 훑는다.
    private List<GameObject> QueryAllies()
    {
        var found = new List<GameObject>();
        Vector2Int originCell = board.WorldToCell(transform.position);
        foreach (Tile tile in TileShapeQuery.GetTiles(board, originCell, radius, shape))
        {
            GameObject occupant = tile.OccupantObject;
            if (occupant != null && tile.OccupantHero is Hero ally && !ally.IsDead)
                found.Add(occupant);
        }
        return found;
    }

    // Hero.GetObjectsInRange(RangeQueryAffinity.Enemy)와 동일한 로직.
    private List<GameObject> QueryEnemies(EnemyAttribute areaUnattackableMask)
    {
        var found = new List<GameObject>();
        var seen = new HashSet<GameObject>();
        Vector2Int originCell = board.WorldToCell(transform.position);
        foreach (Tile tile in TileShapeQuery.GetTiles(board, originCell, radius, shape))
            foreach (GameObject enemy in tile.Enemies)
            {
                if (enemy == null || !seen.Add(enemy)) continue;
                if (PassesAreaMask(enemy, areaUnattackableMask))
                    found.Add(enemy);
            }
        return found;
    }

    private static bool PassesAreaMask(GameObject enemy, EnemyAttribute mask)
    {
        if (mask == EnemyAttribute.None) return true;
        var eb = enemy.GetComponent<EnemyBase>();
        return eb == null || (eb.Attribute & mask) == 0;
    }

    private void SpawnHitEffect(Vector3 pos)
    {
        if (hitEffect == null) return;
        GameObject go = Instantiate(hitEffect, pos, Quaternion.identity);
        if (hitEffectLifetime > 0f) Destroy(go, hitEffectLifetime);
    }

    private void SpawnSelfEffect()
    {
        if (selfEffect == null) return;
        selfEffectInstance = Instantiate(selfEffect, transform.position, selfEffect.transform.rotation);
        // 파티클 프리팹은 프로젝트 관례상 Play On Awake가 꺼져 있다 — Hero.SpawnPersistentEffect와
        // 마찬가지로 직접 Play()를 걸어줘야 실제로 재생된다(안 그러면 스폰만 되고 안 보인다).
        foreach (ParticleSystem ps in selfEffectInstance.GetComponentsInChildren<ParticleSystem>(true))
            ps.Play(true);
        if (selfEffectVisualRadius > 0f)
            selfEffectInstance.transform.localScale = Vector3.one * (radius / selfEffectVisualRadius);
    }

    private void ApplyVisualScale()
    {
        if (visualRadius <= 0f) return;
        transform.localScale = Vector3.one * (radius / visualRadius);
    }

    private void FitParticlesToDuration(float targetDuration)
    {
        foreach (ParticleSystem ps in GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = ps.main;
            if (main.loop || main.duration <= 0f) continue;
            main.simulationSpeed = main.duration / targetDuration;
        }
    }
}
