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

    // 슬롯 하나의 비용을 읽는다. 종류를 모르거나 비용을 못 찾으면 false.
    // 종류마다 비용을 든 컴포넌트가 다르다 — 그 짝을 여기 한 곳에만 적는다(부르는 쪽마다 적으면 갈린다).
    public static bool TryGet(Placeable slot, out PlaceCost cost)
    {
        cost = default;

        if (slot.prefab == null)
        {
            return false;   // 프리팹 없는 슬롯은 저작 실수 — UnitPlacer.CheckPrefab이 배치 시점에 알린다
        }

        switch (slot.kind)
        {
            case OccupantKind.MeleeHero:
            case OccupantKind.RangedHero:
                if (slot.prefab.TryGetComponent(out Hero hero))
                {
                    cost = new PlaceCost(hero.Cost, hero.CitizenAmount);
                }
                break;

            case OccupantKind.Building:
                if (slot.prefab.TryGetComponent(out House house))
                {
                    cost = new PlaceCost(house.Resources, 0);
                }
                break;

            case OccupantKind.Resource:
                if (slot.prefab.TryGetComponent(out ProductionFacility facility))
                {
                    cost = new PlaceCost(facility.GetConstructCost(), 0);
                }
                break;
        }

        return cost.Resources != null;
    }
}
