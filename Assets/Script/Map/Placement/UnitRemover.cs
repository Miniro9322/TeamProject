using System.Collections.Generic;
using UnityEngine;

// 배치된 유닛을 판에서 치우는 담당. 보드에서 떼고, 장부에서 지우고, 풀 반납 또는 파괴한다.
// 한 칸 제거는 그 타일의 보드에서, 전체 제거는 모든 모듈 보드를 돌며 치운다.
public class UnitRemover
{
    private readonly List<MapBoard> _boards;
    private readonly UnitList _unitList;

    public UnitRemover(List<MapBoard> boards, UnitList unitList)
    {
        _boards = boards;
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
        GameObject unit = tile.Board.RemoveUnit(tile.Coord);   // 그 타일이 속한 모듈 보드에서 뗀다

        _unitList.Remove(unit);
        DestroyOrReturnToPool(unit);
        return unit;
    }

    // 모든 모듈 판 위의 모든 유닛을 치운다.
    // 주의: 여기서는 풀 반납 없이 전부 Destroy 한다(기존 동작 보존 — 생산건물 풀 오염 결함).
    public void RemoveAll()
    {
        foreach (MapBoard board in _boards)
        {
            foreach (Tile tile in board.Cells.Values)
            {
                if (tile.OccupantObject == null) continue;
                GameObject unit = board.RemoveUnit(tile.Coord);
                if (unit != null) Object.Destroy(unit);
            }
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
