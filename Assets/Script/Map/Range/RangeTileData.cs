using System.Collections.Generic;

// 계산된 사거리 칸을 들고 있다가 내준다. 계산하거나 표시하지는 않는다.
public class RangeTileData
{
    private readonly List<Tile> tiles = new();

    public IReadOnlyList<Tile> Tiles => tiles;

    // 내용이 바뀔 때마다 올라간다 — TilePaintView가 다시 그릴지 판단하는 값.
    public int Version { get; private set; }

    // 지금 든 범위가 모닥불 것인가 — 유닛 사거리는 채우고, 모닥불은 외곽선만 그리는 갈림에 쓴다.
    public bool IsCampfireRange { get; private set; }

    public void KeepRange(List<Tile> range, bool isCampfire)
    {
        tiles.Clear();
        tiles.AddRange(range);
        IsCampfireRange = isCampfire;
        Version++;
    }

    public void ClearRange()
    {
        tiles.Clear();
        IsCampfireRange = false;
        Version++;
    }
}
