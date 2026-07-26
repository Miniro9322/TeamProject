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

    // 슬롯을 칸에 놓는다: 생성→보드 배치→(성공 시)장부·커버 등록.
    // 성공하면 놓인 오브젝트를 반환(true), 실패하면 만든 오브젝트를 파괴하고 사유를 돌려준다(false).
    public bool TryPlace(Tile tile, Placeable slot, float yOffset, out GameObject placedUnit, out string failReason)
    {
        placedUnit = null;

        GameObject unit = Create(slot);      // 배치할 오브젝트 생성
        if (unit == null)
        {
            failReason = "오브젝트 생성 실패";
            return false;
        }

        BindBoard(unit, tile.Board);         // 생성한 오브젝트에 "놓이는 타일의" 모듈 보드 참조 전달

        if (tile.Board.TryPlace(tile.Coord, unit, slot.kind, yOffset, out failReason))
        {
            RegisterUnit(unit, tile, slot);   // 성공 → 사거리 장부·커버 등록
            placedUnit = unit;
            return true;
        }

        // 실패 → 만든 오브젝트 파괴.
        // (보존 결함) 풀에서 대여한 생산건물도 여기선 반납 없이 Destroy → 풀 오염. 원래 동작이라 그대로 둠.
        if (unit != null) UnityEngine.Object.Destroy(unit);
        return false;
    }

    // 슬롯의 프리팹으로 오브젝트를 만든다(생산건물은 자원 확인 후 풀에서 대여, 그 외는 새로 생성).
    private GameObject Create(Placeable slot)
    {
        CheckPrefab(slot);

        if (slot.kind == OccupantKind.Resource)
        {
            ProductionFacility facility = slot.prefab.GetComponent<ProductionFacility>();
            if (facility != null)
            {
                if (!resourcesManager.CheckResources(facility.BasicValue.ConstructProduct))
                {
                    return null;   // 자원 부족 → 생성하지 않음
                }

                return pool.Rent(facility.ProductionType);
            }
            else return null;
            // (보존 결함) 자원이 모자라도 아래로 떨어져 프리팹을 그냥 생성함. 원래 동작이라 그대로 둠.
        }
        else if(slot.kind == OccupantKind.Building)
        {
            House house = slot.prefab.GetComponent<House>();
            if (house != null)
            {
                if (!resourcesManager.CheckResources(house.Resources))
                {
                    return null;   // 자원 부족 → 생성하지 않음
                }

                return resolver.Instantiate(slot.prefab);
            }
            else return null;
        }
        else
        {
            // 영웅은 로스터 "생성" 단계(HeroSetPanel)에서 이미 비용을 치렀으므로 여기선 다시 검사하지 않는다.
            var hero = slot.prefab.GetComponent<Hero>();
            if (hero != null)
            {
                return resolver.Instantiate(slot.prefab);
            }
            else return null;
        }
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
    private void RegisterUnit(GameObject unit, Tile tile, Placeable slot)
    {
        unitList.Add(unit, slot.attackRange);
        RegisterCover(unit, tile, slot.kind, slot.attackRange);
    }

    // 유닛이 덮는 사거리 칸을 자기 모듈 판에만 표시한다(건물은 사거리 없음 → 제외).
    private static void RegisterCover(GameObject unit, Tile tile, OccupantKind kind, int range)
    {
        if (kind == OccupantKind.Building) return;

        tile.Board.SetRangeCover(unit, tile.Coord, Mathf.Max(0, range));
    }

    private static void CheckPrefab(Placeable slot)
    {
        if (slot.prefab == null)
        {
            throw new MissingReferenceException($"{slot.label} 프리팹이 없습니다.");
        }
    }
}
