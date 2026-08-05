using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 지속 피해(독·점화·출혈) 전역 장부. 대상을 IDamageAble로 받아 적·영웅을 같은 경로로 처리한다.
///
/// 상태를 대상 안에 두지 않는 이유 — Hero.cs를 수정할 수 없고(팀원 소유), 시전자가 죽거나 풀에 반납돼도
/// 디버프는 남아야 한다. 여기서 (대상, 종류, 만료시각)을 들고 굴린다.
/// 영웅 전용이던 PoisonRegistry를 대상만 IDamageAble로 넓혀 옮겨온 것이다(그쪽은 삭제됨).
///
/// 제거 경로가 생명선이다. 만료·사망(Hp<=0)·비활성(풀 반납)·파괴 네 경로 모두에서 장부에서 빠진다 —
/// 하나라도 빠뜨리면 디스폰된 적을 계속 물고 있다가 그 오브젝트를 재사용한 다른 적을 때린다.
/// 영웅 부활은 Hp<=0에서 이미 빠졌기 때문에 어젯밤 독을 물고 나오지 않는다(OnBreak 구독이 하던 역할).
/// </summary>
public static class DotRegistry
{
    private class Entry
    {
        public IDamageAble Target;
        public Component Host;     // 파괴·비활성 감지용(IDamageAble로는 Unity의 생존 여부를 볼 수 없다)
        public DebuffTracker Ledger;   // 제거 시 조회 비트도 같이 끈다. 대상이 IDebuffCarrier가 아니면 null
        public DebuffType Type;
        public int Damage;
        public float Interval;
        public float Expiry;
        public float TickTimer;
    }

    private static readonly List<Entry> entries = new();
    private static GameManager subscribedGm;

    // 장부(DebuffTracker)가 없는 대상의 이펙트 표시용 버퍼. 매 프레임 재사용해 할당을 피한다.
    private static readonly Dictionary<Component, DebuffType> ledgerlessMasks = new();

    // 도메인 리로드를 끈 플레이 모드에서 이전 세션 장부가 남는 것을 막는다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        entries.Clear();
        ledgerlessMasks.Clear();
        subscribedGm = null;
        DotDriver.ResetInstance();
    }

    /// <summary>
    /// 대상에게 type 종류의 지속 피해를 건다. 같은 대상·같은 종류가 이미 있으면 쌓지 않고 갱신한다
    /// (더 센 틱 피해와 더 긴 지속시간이 이긴다). 종류가 다르면 따로 쌓인다 — 독과 점화는 동시에 진행된다.
    /// </summary>
    public static void Apply(IDamageAble target, DebuffType type, int damagePerTick, float interval, float duration, GameManager gm)
    {
        if (target == null || damagePerTick <= 0 || duration <= 0f)
        {
            DebuffDebug.Log($"DotRegistry {type} 거부 — target={(target != null ? "O" : "null")}, 틱피해={damagePerTick}, 지속={duration}");
            return;
        }
        if (target.Hp <= 0f)
        {
            DebuffDebug.Log($"DotRegistry {type} 거부 — 대상 Hp가 {target.Hp} (이미 사망)");
            return;
        }

        Component host = target as Component;
        if (host == null)
        {
            // Unity 생존 여부를 못 보는 대상은 제거 경로를 보장할 수 없어 받지 않는다
            DebuffDebug.Log($"DotRegistry {type} 거부 — 대상이 Component가 아니라 생존 판정 불가");
            return;
        }

        DotDriver.Ensure();
        SubscribeDayReset(gm);
        
        Entry e = Find(target, type);
        bool isNew = e == null;
        if (isNew)
        {
            e = new Entry
            {
                Target = target,
                Host = host,
                Type = type,
                Ledger = (host as IDebuffCarrier ?? host.GetComponentInParent<IDebuffCarrier>())?.Debuffs,
            };
            entries.Add(e);
        }
        e.Damage = Mathf.Max(e.Damage, damagePerTick);
        e.Interval = Mathf.Max(0.05f, interval);
        e.Expiry = Mathf.Max(e.Expiry, Time.time + duration);

        DebuffDebug.Log($"DotRegistry {host.name} {type} {(isNew ? "신규" : "갱신")} — {e.Damage}피해/{e.Interval}초," +
            $" {e.Expiry - Time.time:F1}초 남음 (진행중 {entries.Count}건)", host);
    }

    public static void Clear(IDamageAble target)
    {
        for (int i = entries.Count - 1; i >= 0; i--)
            if (entries[i].Target == target) RemoveAt(i);
    }

    internal static void Tick()
    {
        // 역순 순회 — 틱 도중 제거해도 인덱스가 밀리지 않게(BuffManager.Tick과 같은 패턴).
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            Entry e = entries[i];

            // 파괴 / 풀 반납(비활성) / 사망 / 만료 → 장부에서 제거.
            // 어느 사유로 빠졌는지가 "독이 중간에 끊겼다"를 추적하는 핵심 정보다.
            string reason =
                e.Host == null ? "대상 파괴됨"
                : !e.Host.gameObject.activeInHierarchy ? "대상 비활성(풀 반납)"
                : e.Target.Hp <= 0f ? "대상 사망(Hp 0)"
                : Time.time >= e.Expiry ? "지속시간 만료"
                : null;
            if (reason != null)
            {
                DebuffDebug.Log($"DotRegistry {(e.Host != null ? e.Host.name : "(파괴됨)")} {e.Type} 해제 — {reason}");
                RemoveAt(i);
                continue;
            }

            // Time.deltaTime이라 GameSpeedUI의 timeScale(배속·일시정지)을 그대로 따라간다.
            e.TickTimer += Time.deltaTime;
            if (e.TickTimer < e.Interval) continue;

            e.TickTimer = 0f;
            // 방어력 감산 없이 그대로 넣는다 — Hero.Defense는 NotImplementedException을 던지므로
            // 공통 "방어력 적용" 경로를 절대 타면 안 된다.
            float hpBefore = e.Target.Hp;
            e.Target.TakeDamage(e.Damage);
            DebuffDebug.Log($"DotRegistry {e.Host.name} {e.Type} 틱 {e.Damage}피해 — Hp {hpBefore:F0}→{e.Target.Hp:F0}," +
                $" {e.Expiry - Time.time:F1}초 남음", e.Host);
        }

        SyncLedgerlessEffects();
    }

    // 장부가 없는 대상(영웅)은 디버프 이펙트를 굴려 줄 곳이 없다 — 여기서 대신 밀어 준다.
    // 적은 DebuffTracker가 있어 EnemyDebuffEffects가 프리팹 앵커로 그리므로 제외한다(이중 표시 방지).
    // 제거는 DebuffEffectView가 "이번 프레임 목록에 없으면 반납"으로 처리하므로 해제 통보가 따로 필요 없다.
    private static void SyncLedgerlessEffects()
    {
        ledgerlessMasks.Clear();
        for (int i = 0; i < entries.Count; i++)
        {
            Entry e = entries[i];
            if (e.Ledger != null || e.Host == null) continue;

            ledgerlessMasks.TryGetValue(e.Host, out DebuffType mask);
            ledgerlessMasks[e.Host] = mask | e.Type;   // 독·점화가 같이 걸려 있으면 OR로 합친다
        }
        DebuffEffectView.Sync(ledgerlessMasks);
    }

    private static Entry Find(IDamageAble target, DebuffType type)
    {
        for (int i = 0; i < entries.Count; i++)
            if (entries[i].Target == target && entries[i].Type == type) return entries[i];
        return null;
    }

    // 낮이 되면 전부 해제. 같은 GameManager에 두 번 구독하지 않게 막는다 —
    // 엔트리가 생길 때마다 += 하면 날이 바뀔 때마다 핸들러가 쌓인다.
    private static void SubscribeDayReset(GameManager gm)
    {
        if (gm == null || subscribedGm == gm) return;

        if (subscribedGm != null) subscribedGm.ChangeToDay -= ClearAll;
        gm.ChangeToDay += ClearAll;
        subscribedGm = gm;
    }

    private static void ClearAll()
    {
        for (int i = entries.Count - 1; i >= 0; i--) RemoveAt(i);
    }

    // 장부에서 빼면서 조회 비트도 끈다. 자연 만료는 DebuffTracker도 같은 시각에 풀리므로 무해하고,
    // 낮 전환(ClearAll)처럼 만료 전에 걷어내는 경로에서 살아남은 유닛에 비트가 남는 것을 막는다.
    private static void RemoveAt(int index)
    {
        entries[index].Ledger?.Remove(entries[index].Type);
        entries.RemoveAt(index);
    }
}
