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
        public IUnit Unit;         // 최대 체력 출처. 틱마다 여기서 다시 읽는다 — 아래 MaxHp 주석 참조
        public Component Host;     // 파괴·비활성 감지용(IDamageAble로는 Unity의 생존 여부를 볼 수 없다)
        public DebuffTracker Ledger;   // 제거 시 조회 비트도 같이 끈다. 대상이 IDebuffCarrier가 아니면 null
        public DebuffType Type;
        public float Percent;      // 한 틱에 넣을 대상 최대 체력 비율(%). 2 = 2%
        public float Interval;
        public float Expiry;
        public float TickTimer;
    }

    /// <summary>
    /// 대상의 최대 체력. Hero와 EnemyBase가 함께 구현하는 IUnit의 StatContainer에서 읽는다.
    /// IDamageAble에 MaxHp를 넣지 않는 이유 — 그 인터페이스는 Hero도 구현하므로 넓히면 팀원 파일을 건드려야 한다.
    /// StatType.HP는 양쪽 다 초기화 때 반드시 AddStat하므로(EnemyBase.ApplyData / Hero) 조회가 비지 않는다.
    /// </summary>
    public static float MaxHp(IUnit unit) => unit?.Stats != null ? unit.Stats[StatType.HP] : 0f;

    /// <summary>
    /// 비율(2 = 2%)을 실제 피해값으로 바꾼다. 최소 1은 보장한다 —
    /// 0이 되면 아무 일도 안 일어나 "디버프가 조용히 사라진 것"처럼 보인다.
    /// </summary>
    public static int ToDamage(float maxHp, float percent) =>
        Mathf.Max(1, Mathf.RoundToInt(maxHp * percent * 0.01f));

    /// <summary>
    /// 이 오브젝트 최대 체력의 percent%에 해당하는 피해값. 지속 피해가 아닌 즉발 피해도
    /// 같은 식을 쓰게 공개한다(화염 오라 FireZoneSO — 점화와 수치를 공유하므로 환산도 같아야 한다).
    /// </summary>
    public static int PercentDamage(GameObject target, float percent) =>
        ToDamage(MaxHp(target != null ? target.GetComponentInParent<IUnit>() : null), percent);

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
    /// (더 센 틱 비율과 더 긴 지속시간이 이긴다). 종류가 다르면 따로 쌓인다 — 독과 점화는 동시에 진행된다.
    ///
    /// percentPerTick은 고정 피해가 아니라 <b>대상 최대 체력의 %</b>다(2 = 2%).
    /// 실제 피해값은 틱마다 다시 계산하므로 최대 체력이 변하면 남은 틱부터 따라간다.
    /// </summary>
    /// <returns>장부에 올라갔으면 true. 거절되면 false — 호출부가 아이콘까지 취소할 수 있게 결과를 돌려준다.</returns>
    public static bool Apply(IDamageAble target, DebuffType type, float percentPerTick, float interval, float duration, GameManager gm)
    {
        if (target == null || percentPerTick <= 0f || duration <= 0f)
        {
            DebuffDebug.Log($"DotRegistry {type} 거부 — target={(target != null ? "O" : "null")}, 틱비율={percentPerTick}%, 지속={duration}");
            return false;
        }
        if (target.Hp <= 0f)
        {
            DebuffDebug.Log($"DotRegistry {type} 거부 — 대상 Hp가 {target.Hp} (이미 사망)");
            return false;
        }

        Component host = target as Component;
        if (host == null)
        {
            // Unity 생존 여부를 못 보는 대상은 제거 경로를 보장할 수 없어 받지 않는다
            DebuffDebug.Log($"DotRegistry {type} 거부 — 대상이 Component가 아니라 생존 판정 불가");
            return false;
        }

        // 비율 피해라 최대 체력을 못 읽으면 피해량을 낼 수 없다. 여기서 걸러야
        // 아무 피해도 안 들어가는 디버프가 아이콘으로만 남는 상태를 막는다.
        IUnit unit = target as IUnit ?? host.GetComponentInParent<IUnit>();
        if (MaxHp(unit) <= 0f)
        {
            DebuffDebug.Log($"DotRegistry {type} 거부 — {host.name}에서 최대 체력(IUnit.Stats[HP])을 못 읽어 비율 피해를 낼 수 없다", host);
            return false;
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
                Unit = unit,
                Host = host,
                Type = type,
                Ledger = (host as IDebuffCarrier ?? host.GetComponentInParent<IDebuffCarrier>())?.Debuffs,
            };
            entries.Add(e);
        }
        e.Percent = Mathf.Max(e.Percent, percentPerTick);
        e.Interval = Mathf.Max(0.05f, interval);
        e.Expiry = Mathf.Max(e.Expiry, Time.time + duration);

        DebuffDebug.Log($"DotRegistry {host.name} {type} {(isNew ? "신규" : "갱신")} — 최대체력 {e.Percent:F2}%" +
            $"(={ToDamage(MaxHp(e.Unit), e.Percent)}피해)/{e.Interval}초," +
            $" {e.Expiry - Time.time:F1}초 남음 (진행중 {entries.Count}건)", host);
        return true;
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
            // 최대 체력은 걸 때가 아니라 틱마다 다시 읽는다 — 도중에 최대 체력 버프가 붙거나 풀려도
            // 남은 틱이 그 값을 따라간다(걸 때 한 번 굳히면 버프 창과 어긋난다).
            float maxHp = MaxHp(e.Unit);
            int damage = ToDamage(maxHp, e.Percent);
            float hpBefore = e.Target.Hp;
            e.Target.TakeDamage(damage);
            DebuffDebug.Log($"DotRegistry {e.Host.name} {e.Type} 틱 최대체력 {e.Percent:F2}%={damage}피해" +
                $"(최대 {maxHp:F0}) — Hp {hpBefore:F0}→{e.Target.Hp:F0}," +
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
