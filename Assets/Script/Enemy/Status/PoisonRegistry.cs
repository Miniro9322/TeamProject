using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 영웅에게 걸린 독(지속 피해) 전역 장부.
///
/// 독 상태는 원래 "맞은 영웅"이 들고 있어야 하지만(건 적이 죽어도 독은 남아야 하므로)
/// Hero.cs를 수정할 수 없어 영웅 안에 필드를 둘 수 없다. 그래서 여기서 (영웅, 만료시각)을 대신 들고 굴린다.
/// 시전자(SpiderToxin) 수명과 완전히 분리돼 있어 거미가 죽거나 풀에 반납돼도 독은 그대로 남는다.
///
/// 제거 경로가 생명선이다 — EnemyRegistry가 Unregister를 아무도 안 불러 디스폰된 적을 계속 물고 있는
/// 전례가 있으므로, 여기선 만료·사망·파괴 세 경로 모두에서 반드시 장부에서 빠지게 했다.
/// </summary>
public static class PoisonRegistry
{
    private class Entry
    {
        public Hero Hero;
        public int Damage;        // 틱당 피해
        public float Interval;    // 틱 간격(초)
        public float Expiry;      // Time.time 기준 만료 시각
        public float TickTimer;
        public Action EndHandler; // Hero.OnBreak/OnResur 구독 핸들러 — 제거 시 반드시 해제
    }

    private static readonly List<Entry> entries = new();

    // 도메인 리로드를 끈 플레이 모드(Enter Play Mode Options)에서 이전 세션 장부가 남는 것을 막는다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        entries.Clear();
        PoisonDriver.ResetInstance();
    }
    private static GameManager gameManager;

    /// <summary>
    /// 영웅에게 독을 건다. 이미 중독된 영웅이면 새로 쌓지 않고 갱신한다(거미 5마리가 물어도 독은 하나).
    /// 더 센 틱 피해와 더 긴 지속시간이 이긴다.
    /// </summary>
    public static void Apply(Hero hero, int damagePerTick, float interval, float duration,GameManager gm)
    {
        if (hero == null || hero.IsDead || damagePerTick <= 0 || duration <= 0f) return;

        PoisonDriver.Ensure(); // 첫 사용 시점에 틱 구동체 자동 생성 — 씬에 미리 배치할 필요 없음

        Entry e = Find(hero);
        if (e == null)
        {
            e = new Entry { Hero = hero };
            // 사망/부활 시 즉시 만료 처리 → 다음 Tick에서 장부에서 빠진다.
            // 특히 부활: 낮에 되살아난 영웅이 어젯밤 독을 물고 나오지 않게 한다.
            // Hero를 수정하지 않고 이 보장을 걸 수 있는 유일한 지점(OnBreak/OnResur가 public event).
            e.EndHandler = () => e.Expiry = 0f;
            hero.OnBreak += e.EndHandler;
            hero.OnResur += e.EndHandler;
            entries.Add(e);
            gm.ChangeToDay+= ClearAll;
            gameManager = gm;
        }

        e.Damage = Mathf.Max(e.Damage, damagePerTick);
        e.Interval = Mathf.Max(0.05f, interval);
        e.Expiry = Mathf.Max(e.Expiry, Time.time + duration);
    }

    // 해독·중독 아이콘 등이 필요해지면 Clear(hero)/IsPoisoned(hero)를 여기 추가한다.
    // 지금은 쓰는 곳이 없어 일부러 두지 않았다 — 호출자 없는 공개 API가 EnemyRegistry.Unregister를 썩게 만든 원인.

    // 구동체(PoisonDriver)가 매 프레임 호출한다.
    internal static void Tick()
    {
        // 역순 순회 — 틱 도중 제거해도 인덱스가 밀리지 않게(BuffManager.Tick과 같은 패턴).
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            Entry e = entries[i];

            // 파괴됨(UnitRemover가 Destroy) / 사망 / 만료 → 장부에서 제거
            if (e.Hero == null || e.Hero.IsDead || Time.time >= e.Expiry)
            {
                Remove(i);
                continue;
            }

            // Time.deltaTime이라 GameSpeedUI의 timeScale(배속·일시정지)을 그대로 따라간다.
            e.TickTimer += Time.deltaTime;
            if (e.TickTimer < e.Interval) continue;

            e.TickTimer = 0f;
            // 방어력 감산 없이 그대로 넣는다 — Hero.Defense는 NotImplementedException을 던지므로
            // 공통 "방어력 적용" 경로를 절대 타면 안 된다.
            e.Hero.TakeDamage(e.Damage); // 이 피해로 죽으면 Hero.TakeDamage가 Die() 처리 → 다음 프레임에 제거
            Debug.Log($"[PosionTick]{e.Damage} 데미지 주는중");
        }
    }

    private static Entry Find(Hero hero)
    {
        if (hero == null) return null;
        for (int i = 0; i < entries.Count; i++)
            if (entries[i].Hero == hero) return entries[i];
        return null;
    }

    // 장부에서 빼면서 반드시 이벤트 구독을 푼다. 이걸 빠뜨리면 죽은 영웅의 핸들러가 계속 붙어 있게 된다.
    // (Hero가 이미 Destroy된 경우 Unity의 == null이 true라 건너뛰는데, 그땐 Hero 객체 자체가
    //  이 장부 외엔 참조가 없어 이벤트 목록째로 GC된다.)
    private static void Remove(int index)
    {
        Entry e = entries[index];
        if (e.Hero != null && e.EndHandler != null)
        {
            e.Hero.OnBreak -= e.EndHandler;
            e.Hero.OnResur -= e.EndHandler;
        }
        entries.RemoveAt(index);
    }
    private static void ClearAll()   // 아까 지운 그 메서드 — 이제 호출자가 생기므로 private으로 부활
    {
        for (int i = entries.Count - 1; i >= 0; i--) Remove(i);
    }
}
