using System.Collections.Generic;
using UnityEngine;

// MapBoard.GetTiles(origin, range, bool square)만 사용해 Cross/Line 조회를 조합하는 헬퍼.
// MapBoard 자체에는 새 필드/메서드를 추가하지 않는다.
public static class TileShapeQuery
{
    public static List<Tile> GetTiles(MapBoard board, Vector2Int origin, int range, RangeShape shape)
    {
        if (shape == RangeShape.Diamond) return board.GetTiles(origin, range, false);
        if (shape == RangeShape.Square) return board.GetTiles(origin, range, true);

        // Cross: 정사각 블록을 받아 축(가로/세로) 위의 타일만 남긴다.
        List<Tile> block = board.GetTiles(origin, range, true);
        var result = new List<Tile>();
        foreach (Tile tile in block)
        {
            Vector2Int d = tile.Coord - origin;
            if (d.x == 0 || d.y == 0) result.Add(tile);
        }
        return result;
    }

    // origin 다음 칸부터 direction(단위 벡터, 4방향)으로 length칸 조회. origin 자신은 포함하지 않는다.
    public static List<Tile> GetLineTiles(MapBoard board, Vector2Int origin, Vector2Int direction, int length)
    {
        List<Tile> block = board.GetTiles(origin, length, true);
        var result = new List<Tile>(length);
        for (int step = 1; step <= length; step++)
        {
            Vector2Int target = origin + direction * step;
            Tile tile = block.Find(t => t.Coord == target);
            if (tile != null) result.Add(tile);
        }
        return result;
    }
}
public enum RangeShape { Diamond, Square, Cross }