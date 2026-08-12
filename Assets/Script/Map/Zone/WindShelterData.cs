using System.Collections.Generic;

public class WindShelterData
{
    private readonly Dictionary<Tile, WindShelter> shelterByTile = new();
    private Dictionary<int, int> rowLeft;
    private Dictionary<int, int> rowRight;
    private Dictionary<int, int> colBottom;
    private Dictionary<int, int> colTop;

    // 이 타일의 바람 막힘 정보를 저장한다.
    public void KeepShelter(Tile tile, WindShelter shelter)
    {
        shelterByTile[tile] = shelter;
    }

    // 이 타일의 바람 막힘 정보를 꺼내온다.
    public WindShelter ReadShelter(Tile tile)
    {
        return shelterByTile[tile];
    }

    // WindShelterCalc가 만든 방향별 고지 위치 표를 그대로 넘겨받는다.
    public void KeepLines(Dictionary<int, int> rowLeft, Dictionary<int, int> rowRight, Dictionary<int, int> colBottom, Dictionary<int, int> colTop)
    {
        this.rowLeft = rowLeft;
        this.rowRight = rowRight;
        this.colBottom = colBottom;
        this.colTop = colTop;
    }

    // 이 줄에서 가장 서쪽 고지 X를 꺼내온다.
    public bool TryGetRowLeft(int row, out int x)
    {
        return rowLeft.TryGetValue(row, out x);
    }

    // 이 줄에서 가장 동쪽 고지 X를 꺼내온다.
    public bool TryGetRowRight(int row, out int x)
    {
        return rowRight.TryGetValue(row, out x);
    }

    // 이 줄에서 가장 남쪽 고지 Y를 꺼내온다.
    public bool TryGetColBottom(int col, out int y)
    {
        return colBottom.TryGetValue(col, out y);
    }

    // 이 줄에서 가장 북쪽 고지 Y를 꺼내온다.
    public bool TryGetColTop(int col, out int y)
    {
        return colTop.TryGetValue(col, out y);
    }
}
