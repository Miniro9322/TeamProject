using System.Collections.Generic;
using UnityEngine;

// 배치된 유닛을 판에서 치우는 담당.
public class UnitRemover
{
    private readonly PlacedUnitData _unitList;
    private readonly HeroRoster _heroRoster;

    public UnitRemover(PlacedUnitData unitList, HeroRoster heroRoster)
    {
        _unitList = unitList;
        _heroRoster = heroRoster;
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

        if (IsHouse(unit))
        {
            Debug.Log("집은 재배치만 가능합니다.");
            return null;
        }

        if (!_unitList.TryGetArea(unit, out PlacementArea area))
        {
            return null;
        }

        AreaPlace.Remove(area);
        _unitList.Remove(unit);
        DestroyOrReturnToPool(unit);
        return unit;
    }

    // 판 위의 모든 유닛을 치운다. 풀 반납 없이 전부 파괴한다.
    public void RemoveAll()
    {
        for (int i = 0; i < _unitList.Count; i++)
        {
            AreaPlace.Remove(_unitList.AreaAt(i));
            Object.Destroy(_unitList.UnitAt(i));
        }

        _unitList.Clear();
    }

    // 집인가. 집은 제거가 아니라 재배치만 된다.
    private static bool IsHouse(GameObject unit)
    {
        return unit.TryGetComponent<House>(out _);
    }

    // 생산건물은 풀에 반납하고, 영웅은 로스터로 되돌린 뒤 파괴한다.
    private void DestroyOrReturnToPool(GameObject unit)
    {
        ProductionFacility facility = unit.GetComponent<ProductionFacility>();
        if (facility != null)
        {
            facility.Release();
        }
        else
        {
            HeroRosterLink link = unit.GetComponent<HeroRosterLink>();
            if (link != null && link.Entry != null)
            {
                link.Entry.MarkAvailable();
                _heroRoster.NotifyStateChanged();
            }
            Object.Destroy(unit);
        }
    }
}
