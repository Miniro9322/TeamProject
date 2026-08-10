using UnityEngine;

// 배치된 유닛을 판에서 치우는 담당.
public class UnitRemover
{
    private readonly PlacedUnitData _unitList;
    private readonly HeroRoster _heroRoster;
    private readonly ZoneEffectApplier _zoneEffectApplier;

    public UnitRemover(PlacedUnitData unitList, HeroRoster heroRoster, ZoneEffectApplier zoneEffectApplier)
    {
        _unitList = unitList;
        _heroRoster = heroRoster;
        _zoneEffectApplier = zoneEffectApplier;
    }

    // 한 칸의 유닛을 치운다. 칸이 없으면 false.
    public bool TryRemoveUnit(Tile tile)
    {
        if(tile == null) return false;
        UnitRemove(tile);
        return true;
    }

    public GameObject UnitRemove(Tile tile)
    {
        GameObject unit = tile.OccupantObject;

        if(unit == null) return null;

        if (!_unitList.TryGetArea(unit, out PlacementArea area))
        {
            return null;
        }

        AreaPlace.Remove(area);
        _unitList.Remove(unit);

        if (unit.TryGetComponent(out Hero hero))
        {
            _zoneEffectApplier.ExitZone(hero);
        }

        DestroyOrReturnToPool(unit);
        return unit;
    }

    // 판 위의 모든 유닛을 치운다. 풀 반납 없이 전부 파괴한다.
    public void RemoveAll()
    {
        for (int i = 0; i < _unitList.Count; i++)
        {
            GameObject unit = _unitList.UnitAt(i);
            AreaPlace.Remove(_unitList.AreaAt(i));

            if (unit.TryGetComponent(out Hero hero))
            {
                _zoneEffectApplier.ExitZone(hero);
            }

            Object.Destroy(unit);
        }

        _unitList.Clear();
    }

    // 영웅(로스터 출신)이면 로스터로 되돌리고 파괴.
    // 생산 시설·집은 기반시설 UI(BaseConstructor.Demolish)로 옮겨가 더 이상 맵 유닛으로 존재하지 않는다.
    private void DestroyOrReturnToPool(GameObject unit)
    {
        HeroRosterLink link = unit.GetComponent<HeroRosterLink>();
        if (link != null && link.Entry != null)
        {
            if (unit.TryGetComponent(out Hero hero))
                HeroSelectionService.ClearIfSelected(hero);
            link.Entry.MarkAvailable();
            _heroRoster.NotifyStateChanged();
        }
        Object.Destroy(unit);
    }
}
