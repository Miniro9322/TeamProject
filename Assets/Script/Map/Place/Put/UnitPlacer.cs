using UnityEngine;
using VContainer;
using VContainer.Unity;

// 슬롯의 프리팹으로 유닛을 만들어 판에 놓는 담당.
// 생산 시설·집은 기반시설 UI(BaseConstructor)로 옮겨가 더 이상 맵에 배치되지 않는다 - 여기 남은 건 Hero뿐이다.
public class UnitPlacer
{
    // MapGame이 주입 완료 후 넣어준다.
    public PlacedUnitData unitList;
    public IObjectResolver resolver;
    // MapAssemble이 조립 후 넣어준다.
    public ZoneEffectApplier zoneEffectApplier;

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
        ApplyZoneEffect(unit, data.Area);
        placedUnit = unit;
        return true;
    }

    // 놓인 유닛이 지대 안이면 지대 효과를 건다.
    private void ApplyZoneEffect(GameObject unit, PlacementArea area)
    {
        if (unit.TryGetComponent(out Hero hero))
        {
            zoneEffectApplier.EnterZone(hero, area);
        }
    }

    // 슬롯의 프리팹으로 영웅 오브젝트를 만든다.
    private GameObject Create(Placeable slot)
    {
        CheckPrefab(slot);

        // 영웅은 로스터 "생성" 단계(HeroSetPanel)에서 이미 비용을 치렀으므로 여기선 다시 검사하지 않는다.
        if (!slot.prefab.TryGetComponent(out Hero _))
        {
            return null;
        }

        return resolver.Instantiate(slot.prefab);
    }

    // 유닛에게 자기가 놓인 모듈 보드를 알려준다.
    private static void BindBoard(GameObject unit, MapBoard board)
    {
        if (unit.TryGetComponent(out Hero hero))
        {
            hero.SetBoard(board);
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
