using System.Collections.Generic;
using UnityEngine;

// 불타는 유닛 목록을 굴리며 때리는 장부. 불 위에 서 있는 유닛만 담기므로 아무도 없으면 굴릴 것이 없다.
public static class FireDamage
{
    private const float KeepTime = 3f;

    private class Burning
    {
        public GameObject Unit;
        public IDamageAble Target; // 때릴 문. 장부에 올릴 때 한 번만 찾아 둔다.
        public float NextHit;      // Time.time 기준 다음에 때릴 시각
        public float ExpireAt;
        public bool OnFire;
    }

    private static readonly List<Burning> burnings = new();
    private static readonly Dictionary<GameObject, Burning> byUnit = new();

    private static int damagePerHit;
    private static float hitInterval;

    // 도메인 리로드를 끈 플레이 모드에서 지난 세션의 장부가 남는 것을 막는다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        burnings.Clear();
        byUnit.Clear();
    }

    // 밸런스 숫자를 받아 둔다. FireDriver가 넣는다.
    internal static void SetDamage(int perHit, float seconds)
    {
        damagePerHit = perHit;
        hitInterval = seconds;
    }

    // 이 유닛을 장부에 올린다. 올라선 즉시 한 대 맞는다.
    internal static void Enter(GameObject unit)
    {

        bool exists = byUnit.TryGetValue(unit, out Burning burning);

        if (exists)
        {
            Resume(burning);
            return;
        }

        IDamageAble target = unit.GetComponentInParent<IDamageAble>();
 

        AddBurn(unit, target);
    }

    // 불에서 나온 뒤에도 잠시 피해가 유지되도록 만료 시각을 잡는다.
    internal static void Exit(GameObject unit)
    {
        bool exists = byUnit.TryGetValue(unit, out Burning burning);

        if (!exists)
        {
            return;
        }

        burning.OnFire = false;
        burning.ExpireAt = Time.time + KeepTime;
    }

    // 구동체가 매 프레임 부른다. 불 위에 아무도 없으면 한 바퀴도 돌지 않는다.
    internal static void Tick()
    {
        // 역순 — 도중에 장부에서 빠져도 인덱스가 밀리지 않는다.
        for (int index = burnings.Count - 1; index >= 0; index--)
        {
            Burning burning = burnings[index];

            if (ShouldRemove(burning))
            {
                Remove(index);
                continue;
            }

            Burn(burning);
        }
    }

    // 처음 불에 닿은 유닛을 장부와 조회 사전에 한 번만 등록한다.
    private static void AddBurn(GameObject unit, IDamageAble target)
    {
        var burning = new Burning
        {
            Unit = unit,
            Target = target,
            NextHit = Time.time,
            ExpireAt = float.MaxValue,
            OnFire = true
        };

        byUnit.Add(unit, burning);
        burnings.Add(burning);
    }

    // 지속시간 안에 다시 불에 닿으면 기존 장부를 그대로 사용한다.
    private static void Resume(Burning burning)
    {
        burning.OnFire = true;
        burning.ExpireAt = float.MaxValue;
    }

    // 파괴되거나 죽었거나 불 이탈 지속시간이 끝난 항목인지 확인한다.
    private static bool ShouldRemove(Burning burning)
    {
        if (burning.Unit == null)
        {
            return true;
        }

        if (burning.Target.Hp <= 0f)
        {
            return true;
        }

        if (burning.OnFire)
        {
            return false;
        }

        return Time.time >= burning.ExpireAt;
    }

    // 만료된 항목을 조회 사전과 순회 목록에서 함께 제거한다.
    private static void Remove(int index)
    {
        Burning burning = burnings[index];

        byUnit.Remove(burning.Unit);
        burnings.RemoveAt(index);
    }

    // 때릴 시각이 됐으면 한 대 때리고 다음 시각을 잡는다.
    private static void Burn(Burning burning)
    {
        if (Time.time < burning.NextHit)
        {
            return;
        }

        burning.NextHit = Time.time + hitInterval;
        burning.Target.TakeDamage(damagePerHit);
    }
}
