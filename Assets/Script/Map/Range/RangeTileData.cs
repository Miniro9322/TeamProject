using System.Collections.Generic;

// 계산된 사거리 칸을 들고 있다가 내준다. 계산하거나 표시하지는 않는다.
public class RangeTileData
{
    private readonly List<Tile> tiles = new();

    public IReadOnlyList<Tile> Tiles => tiles;

    public void KeepRange(List<Tile> range)
    {
        tiles.Clear();
        tiles.AddRange(range);
    }

    public void ClearRange()
    {
        tiles.Clear();
    }
}
