using UnityEngine;

// 타일 하나에 칠할 색 하나. 계산도 표시도 하지 않는다.
public readonly struct PaintEntry
{
    public readonly Tile Tile;
    public readonly Color Color;

    public PaintEntry(Tile tile, Color color)
    {
        Tile = tile;
        Color = color;
    }
}
