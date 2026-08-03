using UnityEngine;

// 배치물 하나를 놓는 데 드는 값. 자원과 시민은 같이 봐야 해서 함께 다닌다.
// 얼마인지만 들고 있다 — 낼 수 있는지는 ResourcesManager와 CitizenManager가 본다.
// 값이라 빈 상태(Resources가 null)도 만들어진다. TryGet이 true를 준 것만 쓴다.
public readonly struct PlaceCost
{
    // 깎이는 자원 목록.
    public readonly (ProductionType Type, int Amount)[] Resources;

    // 붙잡아 두는 시민 수. 영웅만 쓰고 건물·시설은 0이다.
    public readonly int Citizens;

    public PlaceCost(
        (ProductionType Type, int Amount)[] resources,
        int citizens)
        {
            Resources = resources;
            Citizens = citizens;
        }

    // 슬롯 하나의 비용을 읽는다. 비용을 못 찾으면 false.
    // 생산 시설·집은 기반시설 UI로 옮겨가 여기선 더 이상 다루지 않는다 - 남은 건 Hero뿐이다.
    public static bool TryGet(Placeable slot, out PlaceCost cost)
    {
        cost = default;

        if (slot.prefab == null)
        {
            return false;   // 프리팹 없는 슬롯은 저작 실수 — UnitPlacer.CheckPrefab이 배치 시점에 알린다
        }

        if (slot.prefab.TryGetComponent(out Hero hero))
        {
            cost = new PlaceCost(hero.Cost, hero.CitizenAmount);
        }

        return cost.Resources != null;
    }
}
