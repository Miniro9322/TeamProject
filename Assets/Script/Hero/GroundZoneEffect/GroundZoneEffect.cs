using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public enum GroundZoneMode { Damage, Heal }

[System.Serializable]
public struct ZoneDebuffEffect
{
    public StatType statType;
    public ModifierType modifierType;
    public float value;
    public int maxStacks;
}

// 장판 프리팹에 직접 붙는 컴포넌트. 예전엔 GroundZoneDataSO(데이터)+GroundZoneRunner(정적 tick 루프)+
// landEffect(별도 참조 프리팹)로 3분할돼 있었지만, 장판은 원래도 "스폰되는 자기 완결 오브젝트"라 프리팹
// 자신이 비주얼이자 틱 로직 주체가 되는 게 더 자연스럽다. Init 직후 스스로 tick을 돌다가 duration이
// 지나면(오라는 소유자가 죽을 때까지) 스스로 풀에 반납한다 — Hero._auraCts 같은 수동 재시작 로직이
// 필요 없어진다(소유자가 죽으면 이 컴포넌트가 다음 루프에서 스스로 감지하고 반납).
[DisallowMultipleComponent]
public class GroundZoneEffect : MonoBehaviour
{
    private const float DebuffGraceInterval = 0.15f; // 다음 틱까지 갱신 못 받으면(영역 이탈) 곧 만료되도록

    public GroundZoneMode mode = GroundZoneMode.Damage;
    public RangeShape shape = RangeShape.Diamond;
    public int radius = 1;
    public float tickInterval = 1f;
    [Tooltip("mode==Damage: 틱당 데미지 = 소유자 ATK * damagePer")]
    public float damagePer = 0.5f;
    [Tooltip("mode==Heal: 틱당 힐량 = 소유자 ATK * healPer (범위 내 최저 체력 아군 1명)")]
    public float healPer = 0f;
    [Tooltip("0 이하 = 오라(소유자가 죽을 때까지 유지). 공격/스킬 트리거형 장판은 반드시 양수로 설정.")]
    public float duration = 3f;
    public List<ZoneDebuffEffect> debuffs = new();
    public GameObject hitEffect;
    public float hitEffectLifetime = 0.5f;
    [Tooltip("이 프리팹의 파티클/데칼이 기본 크기(localScale=1)로 나타내는 반경(타일 수). radius/이값만큼 자기 자신을 스케일한다. 0이면 스케일하지 않음.")]
    public float visualRadius = 0f;

    private MapBoard board;
    private Hero owner;
    private Action<GameObject> release;
    private CancellationTokenSource cts;

    // Hero.SpawnGroundZone이 풀에서 꺼낸 직후 호출한다.
    public void Init(MapBoard board, Hero owner, Action<GameObject> release)
    {
        this.board = board;
        this.owner = owner;
        this.release = release;
        ApplyVisualScale();
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
            release?.Invoke(gameObject);
        }
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
            target.Heal(heal);
            SpawnHitEffect(transform.position);
            return;
        }

        int dmg = Mathf.RoundToInt(owner.SC[StatType.ATK] * damagePer);
        foreach (GameObject go in owner.GetObjectsInRange(transform.position, radius, shape, RangeQueryAffinity.Enemy))
        {
            if (dmg > 0 && go.GetComponentInParent<IDamageAble>() is IDamageAble d)
            {
                d.TakeDamage(dmg);
                owner.NotifyHit(go, dmg, false);
                SpawnHitEffect(go.transform.position);
            }

            if (debuffs.Count > 0 && go.GetComponentInParent<IUnit>() is IUnit unit)
                foreach (ZoneDebuffEffect debuff in debuffs)
                    owner.Buffs.ApplyStackingModifier(unit, debuff.statType, debuff.modifierType,
                        debuff.value, tickInterval + DebuffGraceInterval, debuff.maxStacks, this);
        }
    }

    private void SpawnHitEffect(Vector3 pos) => owner.SpawnEffect(hitEffect, pos, Quaternion.identity, hitEffectLifetime);

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
