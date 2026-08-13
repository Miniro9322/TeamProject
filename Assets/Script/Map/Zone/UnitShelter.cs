using System.Collections.Generic;
using UnityEngine;

// 밤에 읽은 유닛 좌표로 방향별 유닛 가림막을 조회합니다.
public class UnitShelter
{
    private readonly Dictionary<int, int> rowLeft = new();
    private readonly Dictionary<int, int> rowRight = new();
    private readonly Dictionary<int, int> colBottom = new();
    private readonly Dictionary<int, int> colTop = new();
    private readonly HashSet<Vector2Int> unitCells = new();

    // 동적으로 배치된 유닛 좌표를 한 번 읽어 행·열 끝값과 점유 칸을 저장합니다.
    public UnitShelter(IReadOnlyList<Vector2Int> units)
    {
        Collect(units);
    }

    // 대상 바로 한 칸 뒤(바람이 불어온 쪽)에 다른 유닛이 있는지 조회합니다.
    public bool IsSheltered(Vector2Int target, Vector2Int wind)
    {
        return unitCells.Contains(target - wind);
    }

    // 이 행에서 가장 서쪽 유닛 X를 꺼내온다.
    public bool TryGetRowLeft(int row, out int x)
    {
        return rowLeft.TryGetValue(row, out x);
    }

    // 이 행에서 가장 동쪽 유닛 X를 꺼내온다.
    public bool TryGetRowRight(int row, out int x)
    {
        return rowRight.TryGetValue(row, out x);
    }

    // 이 열에서 가장 남쪽 유닛 Y를 꺼내온다.
    public bool TryGetColBottom(int col, out int y)
    {
        return colBottom.TryGetValue(col, out y);
    }

    // 이 열에서 가장 북쪽 유닛 Y를 꺼내온다.
    public bool TryGetColTop(int col, out int y)
    {
        return colTop.TryGetValue(col, out y);
    }

    // 유닛 수가 매번 달라지는 좌표 목록을 한 번 순회해 끝값을 모읍니다.
    private void Collect(IReadOnlyList<Vector2Int> units)
    {
        foreach (Vector2Int unit in units)
        {
            KeepMin(rowLeft, unit.y, unit.x);
            KeepMax(rowRight, unit.y, unit.x);
            KeepMin(colBottom, unit.x, unit.y);
            KeepMax(colTop, unit.x, unit.y);
            unitCells.Add(unit);
        }
    }

    // 같은 줄의 가장 작은 위치를 저장합니다.
    private static void KeepMin(Dictionary<int, int> values, int line, int position)
    {
        if (!values.TryGetValue(line, out int current) || position < current)
        {
            values[line] = position;
        }
    }

    // 같은 줄의 가장 큰 위치를 저장합니다.
    private static void KeepMax(Dictionary<int, int> values, int line, int position)
    {
        if (!values.TryGetValue(line, out int current) || position > current)
        {
            values[line] = position;
        }
    }
}
