using System.Collections.Generic;

// 계산된 사거리 칸을 들고 있다가 내준다. 계산하거나 표시하지는 않는다.
public class RangeTileData
{
    private readonly List<Tile> tiles = new();

    public IReadOnlyList<Tile> Tiles => tiles;

    // 내용이 바뀔 때마다 올라간다 — TilePaintView가 다시 그릴지 판단하는 값.
    public int Version { get; private set; }

    public void KeepRange(List<Tile> range)
    {
        tiles.Clear();
        tiles.AddRange(range);
        Version++;
    }

    public void ClearRange()
    {
        tiles.Clear();
        Version++;
    }
}
