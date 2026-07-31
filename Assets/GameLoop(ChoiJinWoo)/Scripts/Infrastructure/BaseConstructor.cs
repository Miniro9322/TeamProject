using UnityEngine;
using VContainer;
using VContainer.Unity;

// 기반시설 UI에서 곧장 건물을 짓는 담당. UnitPlacer와 달리 PlacementArea/MapBoard가 없다 -
// 지역 슬롯은 좌표를 갖지 않으므로 자원 확인 -> 생성 -> 슬롯 배정까지만 하면 끝.
public class BaseConstructor
{
    private readonly ResourcesManager resourcesManager;
    private readonly BuildingPool pool;
    private readonly IObjectResolver resolver;

    public BaseConstructor(ResourcesManager resourcesManager, BuildingPool pool, IObjectResolver resolver)
    {
        this.resourcesManager = resourcesManager;
        this.pool = pool;
        this.resolver = resolver;
    }

    public bool CanBuild(Placeable slot)
    {
        switch (slot.kind)
        {
            case OccupantKind.Resource:
                var facility = slot.prefab.GetComponent<ProductionFacility>();
                return facility != null && resourcesManager.CheckResources(facility.GetConstructCost());
            case OccupantKind.Building:
                var house = slot.prefab.GetComponent<House>();
                return house != null && resourcesManager.CheckResources(house.Resources);
            default:
                return false;
        }
    }

    public bool TryBuild(Placeable slot, RegionFacilitySlots region, int slotIndex, out GameObject built)
    {
        built = null;
        if (slotIndex < 0 || slotIndex >= region.Slots.Count || !region.Slots[slotIndex].IsEmpty) return false;
        if (!CanBuild(slot)) return false;

        built = slot.kind == OccupantKind.Resource ? BuildFacility(slot) : BuildHouse(slot);
        if (built == null) return false;

        region.TryAssign(slotIndex, built, slot.icon, slot.label);
        return true;
    }

    private GameObject BuildFacility(Placeable slot)
    {
        var prefabFacility = slot.prefab.GetComponent<ProductionFacility>();
        return pool.Rent(prefabFacility.ProductionType);
    }

    private GameObject BuildHouse(Placeable slot)
    {
        return resolver.Instantiate(slot.prefab);
    }

    public void Demolish(RegionFacilitySlots region, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= region.Slots.Count) return;

        var occupant = region.Slots[slotIndex].Occupant;
        if (occupant == null) return;

        region.TryClear(slotIndex);

        var facility = occupant.GetComponent<ProductionFacility>();
        if (facility != null)
        {
            facility.Release();
            return;
        }

        Object.Destroy(occupant);
    }
}
