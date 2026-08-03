using System.Collections.Generic;
using UnityEngine;

// 배치된 유닛을 판에서 치우는 담당. 보드에서 떼고, 장부에서 지우고, 풀 반납 또는 파괴한다.
// 한 칸 제거는 그 타일의 보드에서, 전체 제거는 모든 모듈 보드를 돌며 치운다.
public class UnitRemover
{
    private readonly List<MapBoard> _boards;
    private readonly UnitList _unitList;
    private readonly HeroRoster _heroRoster;

    public UnitRemover(List<MapBoard> boards, UnitList unitList, HeroRoster heroRoster)
    {
        _boards = boards;
        _unitList = unitList;
        _heroRoster = heroRoster;
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
        GameObject unit = tile.OccupantObject;

        if(unit == null) return null;

        AreaPlace.Remove(tile, unit);   // 여러 칸을 덮고 있어도 전부 비운다(어느 칸을 눌러도 같은 결과)

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
                GameObject unit = tile.OccupantObject;
                AreaPlace.Remove(tile, unit);   // 같은 유닛의 나머지 칸도 함께 비워져 아래 순회에서 자연히 건너뛴다
                Object.Destroy(unit);
            }
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
            link.Entry.MarkAvailable();
            _heroRoster.NotifyStateChanged();
        }
        Object.Destroy(unit);
    }
}
