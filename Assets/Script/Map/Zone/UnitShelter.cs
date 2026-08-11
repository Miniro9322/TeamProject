using System;
using System.Collections.Generic;
using UnityEngine;

// 밤에 읽은 유닛 좌표로 방향별 유닛 가림막을 조회합니다.
public class UnitShelter
{
    private readonly Dictionary<int, int> rowMin = new();
    private readonly Dictionary<int, int> rowMax = new();
    private readonly Dictionary<int, int> colMin = new();
    private readonly Dictionary<int, int> colMax = new();

    // 동적으로 배치된 유닛 좌표를 한 번 읽어 행·열 끝값을 저장합니다.
    public UnitShelter(IReadOnlyList<Vector2Int> units)
    {
        Collect(units);
    }

    // 대상보다 바람이 들어오는 쪽에 다른 유닛이 있는지 조회합니다.
    public bool IsSheltered(Vector2Int target, Vector2Int wind)
    {
        if (wind == GridCalculator.Right)
        {
            return HasLower(rowMin, target.y, target.x);
        }

        if (wind == GridCalculator.Left)
        {
            return HasHigher(rowMax, target.y, target.x);
        }

        if (wind == GridCalculator.Up)
        {
            return HasLower(colMin, target.x, target.y);
        }

        if (wind == GridCalculator.Down)
        {
            return HasHigher(colMax, target.x, target.y);
        }

        throw new ArgumentOutOfRangeException(nameof(wind));
    }

    // 유닛 수가 매번 달라지는 좌표 목록을 한 번 순회해 끝값을 모읍니다.
    private void Collect(IReadOnlyList<Vector2Int> units)
    {
        foreach (Vector2Int unit in units)
        {
            KeepMin(rowMin, unit.y, unit.x);
            KeepMax(rowMax, unit.y, unit.x);
            KeepMin(colMin, unit.x, unit.y);
            KeepMax(colMax, unit.x, unit.y);
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

    // 같은 줄의 더 작은 위치에 다른 유닛이 있는지 조회합니다.
    private static bool HasLower(Dictionary<int, int> values, int line, int position)
    {
        return values.TryGetValue(line, out int minimum) && minimum < position;
    }

    // 같은 줄의 더 큰 위치에 다른 유닛이 있는지 조회합니다.
    private static bool HasHigher(Dictionary<int, int> values, int line, int position)
    {
        return values.TryGetValue(line, out int maximum) && maximum > position;
    }
}
