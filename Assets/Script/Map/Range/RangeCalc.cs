using System.Collections.Generic;

// 타일 위에 선 유닛이 닿는 칸들을 계산해 돌려준다. 보관하거나 표시하지는 않는다.
public class RangeCalc
{
    private readonly RangeInfo rangeInfo;

    public RangeCalc(RangeInfo rangeInfo)
    {
        this.rangeInfo = rangeInfo;
    }

    // 그리드 밖이거나 사거리를 가진 유닛이 없으면 빈 목록.
    public List<Tile> GetRange(Tile unitTile)
    {
        if (unitTile == null)
        {
            return new List<Tile>();
        }

        bool hasRange = rangeInfo.TryGet(
            unitTile.OccupantObject,
            out int range,
            out RangeShape shape);

        if (!hasRange)
        {
            return new List<Tile>();
        }

        return TileShapeQuery.GetTiles(
            unitTile.Board,
            unitTile.Coord,
            range,
            shape);
    }
}
