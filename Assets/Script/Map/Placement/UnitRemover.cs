using UnityEngine;

// 배치된 유닛을 판에서 치우는 담당. 보드에서 떼고, 장부에서 지우고, 풀 반납 또는 파괴한다.
public class UnitRemover
{
    private readonly MapBoard _board;
    private readonly UnitList _unitList;

    public UnitRemover(MapBoard board, UnitList unitList)
    {
        _board = board;
        _unitList = unitList;
    }

    // 한 칸의 유닛을 치운다. 치운 유닛을 돌려준다(빈 칸이면 null).
    public bool TryRemoveUnit(Tile tile)
    {
        if(tile == null) return false;
        UnitRemove(tile);
        return true;
    }

    public GameObject UnitRemove(Tile tile)
    {
        GameObject unit = _board.RemoveUnit(tile.Coord);
   
        _unitList.Remove(unit);
        DestroyOrReturnToPool(unit);
        return unit;
    }

    // 판 위의 모든 유닛을 치운다.
    // 주의: 여기서는 풀 반납 없이 전부 Destroy 한다(기존 동작 보존 — 생산건물 풀 오염 결함).
    public void RemoveAll()
    {
        foreach (Tile tile in _board.Cells.Values)
        {
            if (tile.OccupantObject == null) continue;
            GameObject unit = _board.RemoveUnit(tile.Coord);
            if (unit != null) Object.Destroy(unit);
        }
        _unitList.Clear();
    }

    // 생산건물이면 풀에 반납, 아니면 파괴.
    private static void DestroyOrReturnToPool(GameObject unit)
    {
        ProductionFacility facility = unit.GetComponent<ProductionFacility>();
        if (facility != null)
        {
            facility.Release();
        }
        else
        {
            Object.Destroy(unit);
        }
    }
}
