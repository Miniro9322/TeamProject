using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public enum GroundZoneMode { Damage, Heal, Buff }

// 장판 프리팹에 직접 붙는 컴포넌트. 예전엔 GroundZoneDataSO(데이터)+GroundZoneRunner(정적 tick 루프)+
// landEffect(별도 참조 프리팹)로 3분할돼 있었지만, 장판은 원래도 "스폰되는 자기 완결 오브젝트"라 프리팹
// 자신이 비주얼이자 틱 로직 주체가 되는 게 더 자연스럽다. Init 직후 스스로 tick을 돌다가 duration이
// 지나면(오라는 소유자가 죽을 때까지) 스스로 풀에 반납한다 — Hero._auraCts 같은 수동 재시작 로직이
// 필요 없어진다(소유자가 죽으면 이 컴포넌트가 다음 루프에서 스스로 감지하고 반납).
[DisallowMultipleComponent]
public class GroundZoneEffect : MonoBehaviour
{
    public GroundZoneMode mode = GroundZoneMode.Damage;
    public RangeShape shape = RangeShape.Diamond;
    public int radius = 1;
    public float tickInterval = 1f;
    [Tooltip("mode==Damage: 틱당 데미지 = 소유자 ATK * damagePer")]
    public float damagePer = 0.5f;
    [Tooltip("mode==Heal: 틱당 힐량 = 소유자 ATK * healPer (범위 내 최저 체력 아군 1명)")]
    public float healPer = 0f;
    public float hpHealPer = 0f;
    [Tooltip("0 이하 = 오라(소유자가 죽을 때까지 유지). 공격/스킬 트리거형 장판은 반드시 양수로 설정.")]
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
    private Hero owner;
    private bool followOwner;
    private Action<GameObject> release;
    private CancellationTokenSource cts;
    private GameObject selfEffectInstance;
    private readonly HashSet<Hero> buffedAllies = new();

    // Hero.SpawnGroundZone이 풀에서 꺼낸 직후 호출한다. followOwner는 오라(소유자를 따라다녀야 하는
    // 장판)인지, 스킬/공격이 심어놓고 떠나는 장판인지를 호출부가 명시한다(duration 값으로는 구분 불가 —
    // 실제 오라 프리팹도 duration을 999처럼 유한값으로 쓴다).
    public void Init(MapBoard board, Hero owner, bool followOwner, Action<GameObject> release)
    {
        this.board = board;
        this.owner = owner;
        this.followOwner = followOwner;
        this.release = release;
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
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
    }

    private async UniTask RunLifetime(CancellationToken token)
    {
        try
        {
            bool persistent = duration <= 0f;
            float elapsed = 0f, tickTimer = 0f;
            while (!token.IsCancellationRequested && (persistent ? owner != null && !owner.IsDead : elapsed < duration))
            {
                float dt = Time.deltaTime;
                if (!persistent) elapsed += dt;
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

            // this가 이미 파괴된 상태(예: followOwner 오라가 소유자 파괴와 함께 자식으로 같이 파괴된 경우)면
            // gameObject 등 네이티브 접근은 전부 건너뛴다 — 반납할 풀도 소유자와 함께 사라지는 것이므로 안전하다.
            if (this != null)
            {
                if (duration <= 0f && selfEffectInstance != null)
                    owner.DespawnEffect(selfEffect, selfEffectInstance);
                release?.Invoke(gameObject);
            }
        }
    }

    // Buff 모드 장판이 사라질 때(정상 만료/소유자 사망/파괴) 아직 범위 안에 있던 아군의 버프도
    // "지금 나간 것"과 동일하게 정리한다 — 방치되는 버프가 없도록.
    private void ClearAllyBuffs()
    {
        if (buffedAllies.Count == 0) return;
        foreach (Hero ally in buffedAllies)
        {
            if (ally == null) continue;
            foreach (BuffEffect effect in allyBuffs)
                owner.Buffs.RemoveBuff(ally, effect.statType, this);
        }
        buffedAllies.Clear();
    }

    private void Tick()
    {
        if (board == null || owner == null) return;

        if (mode == GroundZoneMode.Heal)
        {
            float heal = owner.SC[StatType.ATK] * healPer;
            if (heal <= 0f) return;
            Hero target = AttackDamageUtil.FindLowestHpAlly(owner.GetObjectsInRange(transform.position, radius, shape, RangeQueryAffinity.Ally));
            if (target == null) return;
            heal = heal + target.SC[StatType.HP] * hpHealPer;
            target.Heal(heal);
            SpawnHitEffect(transform.position);
            return;
        }

        if (mode == GroundZoneMode.Buff)
        {
            var inRange = new HashSet<Hero>();
            foreach (GameObject go in owner.GetObjectsInRange(transform.position, radius, shape, RangeQueryAffinity.Ally))
                if (go.GetComponentInParent<Hero>() is Hero ally)
                    inRange.Add(ally);

            foreach (Hero ally in inRange)
            {
                if (!buffedAllies.Add(ally)) continue;
                foreach (BuffEffect effect in allyBuffs)
                    owner.Buffs.ApplyStackingModifier(ally, effect.statType, effect.modifierType, effect.value, 0f, effect.maxStacks, this);
            }

            buffedAllies.RemoveWhere(ally =>
            {
                if (ally != null && inRange.Contains(ally)) return false;
                if (ally != null)
                    foreach (BuffEffect effect in allyBuffs)
                        owner.Buffs.RemoveBuff(ally, effect.statType, this);
                return true;
            });
            return;
        }

        int dmg = Mathf.RoundToInt(owner.SC[StatType.ATK] * damagePer);
        // 장판(지상)은 공중 적을 절대 때릴 수 없다 — 호출부 설정과 무관한 GroundZoneEffect 자체의 불변식.
        foreach (GameObject go in owner.GetObjectsInRange(transform.position, radius, shape, RangeQueryAffinity.Enemy, EnemyAttribute.Fly))
        {
            if (dmg > 0 && go.GetComponentInParent<IDamageAble>() is IDamageAble d)
            {
                d.TakeDamage(dmg);
                owner.NotifyHit(go, dmg, false);
                SpawnHitEffect(go.transform.position);
            }

            AttackDamageUtil.ApplyTargetDebuffs(go.transform, targetDebuffs, owner.Buffs, this);
        }
    }

    private void SpawnHitEffect(Vector3 pos) => owner.SpawnEffect(hitEffect, pos, Quaternion.identity, hitEffectLifetime);

    private void SpawnSelfEffect()
    {
        if (selfEffect == null) return;
        selfEffectInstance = owner.SpawnEffect(selfEffect, transform.position, selfEffect.transform.rotation, duration > 0f ? duration : 0f);
        if (selfEffectInstance == null) return;
        // 장판 자신의 transform(ApplyVisualScale로 이미 스케일됨)이 아니라 owner에 직접 매달아야
        // 스케일이 중첩되지 않는다.
        if (followOwner)
            selfEffectInstance.transform.SetParent(owner.transform, worldPositionStays: true);
        if (selfEffectVisualRadius > 0f)
            selfEffectInstance.transform.localScale = Vector3.one * (radius / selfEffectVisualRadius);
    }

    private void ApplyVisualScale()
    {
        if (visualRadius <= 0f) return;
        transform.localScale = Vector3.one * (radius / visualRadius);
    }

    // duration<=0(오라)일 땐 호출되지 않는다 — 원래도 반복되는 자식(Trail 등)은 그대로 두고,
    // 1회성(loop=false)으로 authored된 자식만 targetDuration에 맞춰 재생 속도를 조정한다.
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
