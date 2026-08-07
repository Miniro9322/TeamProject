using System.Collections.Generic;
using UnityEngine;

// 타일 위에 선 유닛이 닿는 칸들을 계산해 돌려준다. 보관하거나 표시하지는 않는다.
public class RangeCalc
{
    private readonly RangeInfo rangeInfo;

    public RangeCalc(RangeInfo rangeInfo)
    {
        this.rangeInfo = rangeInfo;
    }

    public bool TryGetRange(Tile unitTile, out List<Tile> range)
    {
        range = new List<Tile>();

        if (HasTile(unitTile))
        {
            return TryGetRangeAtCenter(unitTile.OccupantObject, unitTile, out range);
        }

        return false;
    }

    // 아직 타일에 놓이지 않은 유닛(배치 프리뷰)도 중심 타일을 따로 받아 계산한다.
    public bool TryGetRangeAtCenter(GameObject unit, Tile center, out List<Tile> range)
    {
        range = new List<Tile>();

        if (rangeInfo.TryGet(unit, out int reach, out RangeShape shape))
        {
            range = TileShapeQuery.GetTiles(center.Board, center.Coord, reach, shape);
            return true;
        }

        return false;
    }

    private static bool HasTile(Tile tile)
    {
        return tile != null;
    }
}
