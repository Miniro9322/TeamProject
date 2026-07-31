using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

// 유닛을 새로 만들어 판에 놓는 담당. 프리팹으로 오브젝트를 생성하고, 보드에 배치하고, 사거리 장부·커버를 등록한다.
// MapGame(중재자)이 "이 슬롯을 이 칸에 놓아라" 시키면 생성→배치→등록까지 하고 성공/실패를 돌려준다.
// resolver·pool은 [Inject]로 늦게 들어오므로, MapGame이 Construct(주입 완료) 시점에 이 담당을 만든다.
public class UnitPlacer
{
    // MapGame(조립자)이 [Inject] 완료 후 직접 넣어준다.
    public UnitList unitList;
    public IObjectResolver resolver;
    public ResourcesManager resourcesManager;
    public BuildingPool pool;

    // 슬롯을 자리에 놓는다: 자리 확인→생성→보드 배치→장부·커버 등록.
    // 자리는 한 칸일 수도 여러 칸일 수도 있다(PlacementArea가 덮는 칸을 모두 들고 있다).
    // 자리가 막혔으면 만들기 전에 멈춘다 — 결제·풀 대여를 끝낸 뒤 되돌리는 경로를 두지 않는다.
    public bool TryPlace(PlaceData data, Placeable slot, out GameObject placedUnit)
    {
        placedUnit = null;

        if (!data.CanPlace)
        {
            return false;
        }

        GameObject unit = Create(slot);      // 배치할 오브젝트 생성
        if (unit == null)
        {
            return false;
        }

        BindBoard(unit, data.Area.Board);    // 생성한 오브젝트에 "놓이는 자리의" 모듈 보드 참조 전달
        AreaPlace.Place(data, unit, slot.kind);
        RegisterUnit(unit, data.Area, slot); // 사거리 장부·커버 등록
        placedUnit = unit;
        return true;
    }

    // 슬롯의 프리팹으로 오브젝트를 만든다. 비용을 못 내면 만들기 전에 멈춘다.
    private GameObject Create(Placeable slot)
    {
        CheckPrefab(slot);

        if (!CanAfford(slot))
        {
            return null;
        }

        return Spawn(slot);
    }

    // 영웅은 로스터 "생성" 단계(HeroSetPanel)에서 이미 비용을 치렀으므로 여기선 다시 검사하지 않는다.
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
            return resolver.Instantiate(slot.prefab);   // 집이 없는 슬롯은 CanAfford가 이미 걸렀다
        }

        // 영웅은 비용 검사를 건너뛰므로 여기서 처음 컴포넌트를 확인한다.
        if (!slot.prefab.TryGetComponent(out Hero _))
        {
            return null;
        }

        return resolver.Instantiate(slot.prefab);
    }

    // 생성한 오브젝트에 보드 참조를 넘긴다(유닛이 스스로 보드를 알아야 하는 경우).
    // 자기 모듈 보드만 받으므로 유닛의 타겟 조회가 다른 모듈로 새지 않는다.
    private static void BindBoard(GameObject unit, MapBoard board)
    {
        IPlaceAble placeable = unit.GetComponent<IPlaceAble>();
        if (placeable != null)
        {
            placeable.SetBoard(board);
        }
    }

    // 배치된 유닛의 사거리를 장부에 올리고, 판에 사거리 커버를 등록한다.
    private void RegisterUnit(GameObject unit, PlacementArea area, Placeable slot)
    {
        unitList.Add(unit, slot.attackRange);
        RegisterCover(unit, area, slot.kind, slot.attackRange);
    }

    // 유닛이 덮는 사거리 칸을 자기 모듈 판에만 표시한다(건물은 사거리 없음 → 제외).
    // 여러 칸을 차지해도 커버는 시작 칸 하나를 중심으로 잡는다(사거리가 있는 영웅은 아직 1×1).
    private static void RegisterCover(GameObject unit, PlacementArea area, OccupantKind kind, int range)
    {
        if (kind == OccupantKind.Building) return;

        area.Board.SetRangeCover(unit, area.Origin, Mathf.Max(0, range));
    }

    private static void CheckPrefab(Placeable slot)
    {
        if (slot.prefab == null)
        {
            throw new MissingReferenceException($"{slot.label} 프리팹이 없습니다.");
        }
    }
}
