using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

// 슬롯의 프리팹으로 유닛을 만들어 판에 놓는 담당.
public class UnitPlacer
{
    // MapGame이 주입 완료 후 넣어준다.
    public PlacedUnitData unitList;
    public IObjectResolver resolver;
    public ResourcesManager resourcesManager;
    public BuildingPool pool;

    // 슬롯을 자리에 놓는다. 못 놓으면 false.
    public bool TryPlace(PlaceData data, Placeable slot, out GameObject placedUnit)
    {
        placedUnit = null;

        GameObject unit = Create(slot);
        if (unit == null)
        {
            return false;
        }

        BindBoard(unit, data.Area.Board);
        AreaPlace.Place(data, unit, slot.kind);
        unitList.Add(unit, data.Area);
        placedUnit = unit;
        return true;
    }

    // 슬롯의 프리팹으로 오브젝트를 만든다. 비용을 못 내면 null.
    private GameObject Create(Placeable slot)
    {
        CheckPrefab(slot);

        if (!CanAfford(slot))
        {
            return null;
        }

        return Spawn(slot);
    }

    // 비용을 낼 수 있는가. 영웅은 로스터에서 이미 냈으므로 항상 true.
    private bool CanAfford(Placeable slot)
    {
        if (slot.kind != OccupantKind.Building && slot.kind != OccupantKind.Resource)
        {
            return true;
        }

        return PlaceCost.TryGet(slot, out PlaceCost cost)
            && resourcesManager.CheckResources(cost.Resources);
    }

    // 생산 시설은 풀에서 빌리고, 나머지는 새로 만든다.
    private GameObject Spawn(Placeable slot)
    {
        if (slot.kind == OccupantKind.Resource)
        {
            if (!slot.prefab.TryGetComponent(out ProductionFacility facility))
            {
                return null;
            }

            return pool.Rent(facility.ProductionType);
        }

        if (slot.kind == OccupantKind.Building)
        {
            return resolver.Instantiate(slot.prefab);
        }

        if (!slot.prefab.TryGetComponent(out Hero _))
        {
            return null;
        }

        return resolver.Instantiate(slot.prefab);
    }

    // 유닛에게 자기가 놓인 모듈 보드를 알려준다.
    private static void BindBoard(GameObject unit, MapBoard board)
    {
        IPlaceAble placeable = unit.GetComponent<IPlaceAble>();
        if (placeable != null)
        {
            placeable.SetBoard(board);
        }
    }

    private static void CheckPrefab(Placeable slot)
    {
        if (slot.prefab == null)
        {
            throw new MissingReferenceException($"{slot.label} 프리팹이 없습니다.");
        }
    }
}
