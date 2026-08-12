using System;
using System.Collections.Generic;
using UnityEngine;

public static class WindShelterQuery
{
    // 이 바람 방향으로 막혀 있는지 확인한다.
    public static bool IsSheltered(WindShelter shelter, Vector2Int wind)
    {
        WindShelter currentShelter = ResolveDirection(wind);
        return (shelter & currentShelter) == currentShelter;
    }

    // 이 바람이 이 줄에서 마지막으로 닿는 노출 칸을 찾는다. 고지·유닛 중 더 가까운 벽 바로 앞, 둘 다 없으면 안쪽 맵 끝.
    public static Vector2Int FindLastUnsheltered(WindShelterData terrain, UnitShelter units, RectInt playRect, Vector2Int wind, int line)
    {
        return LastUnshelteredFinders[wind](terrain, units, playRect, line);
    }

    // 바람 벡터를 그 방향을 막는 데 필요한 값으로 바꾼다.
    private static WindShelter ResolveDirection(Vector2Int wind)
    {
        if (wind == GridCalculator.Down) return WindShelter.FromNorth;
        if (wind == GridCalculator.Up) return WindShelter.FromSouth;
        if (wind == GridCalculator.Right) return WindShelter.FromWest;
        if (wind == GridCalculator.Left) return WindShelter.FromEast;

        throw new ArgumentOutOfRangeException(nameof(wind));
    }

    private delegate Vector2Int LastUnshelteredFinder(WindShelterData terrain, UnitShelter units, RectInt playRect, int line);

    // 바람 벡터마다 마지막 노출 칸 계산 방식을 찾아주는 표.
    private static readonly Dictionary<Vector2Int, LastUnshelteredFinder> LastUnshelteredFinders = new()
    {
        { GridCalculator.Right, FindEastUnshelteredCell },
        { GridCalculator.Left, FindWestUnshelteredCell },
        { GridCalculator.Up, FindNorthUnshelteredCell },
        { GridCalculator.Down, FindSouthUnshelteredCell },
    };

    // TryGetValue가 못 찾았으면 이 방향 비교에 영향이 없는 대신 값을 쓴다.
    private static int ValueOrFallback(bool found, int value, int fallback)
    {
        if (found) return value;
        return fallback;
    }

    // 동쪽으로 부는 바람이 이 행(row)에서 마지막으로 닿는 칸. 고지 벽과 유닛 벽 중 더 서쪽(가까운) 쪽을 기준으로 삼는다.
    private static Vector2Int FindEastUnshelteredCell(WindShelterData terrain, UnitShelter units, RectInt playRect, int row)
    {
        bool hasHigh = terrain.TryGetRowLeft(row, out int highX);
        bool hasUnit = units.TryGetRowLeft(row, out int unitX);
        int high = ValueOrFallback(hasHigh, highX, playRect.xMax);
        int unit = ValueOrFallback(hasUnit, unitX, playRect.xMax);
        int wallX = Mathf.Min(high, unit);
        return new Vector2Int(wallX - 1, row);
    }

    // 서쪽으로 부는 바람이 이 행(row)에서 마지막으로 닿는 칸. 고지 벽과 유닛 벽 중 더 동쪽(가까운) 쪽을 기준으로 삼는다.
    private static Vector2Int FindWestUnshelteredCell(WindShelterData terrain, UnitShelter units, RectInt playRect, int row)
    {
        bool hasHigh = terrain.TryGetRowRight(row, out int highX);
        bool hasUnit = units.TryGetRowRight(row, out int unitX);
        int high = ValueOrFallback(hasHigh, highX, playRect.xMin - 1);
        int unit = ValueOrFallback(hasUnit, unitX, playRect.xMin - 1);
        int wallX = Mathf.Max(high, unit);
        return new Vector2Int(wallX + 1, row);
    }

    // 북쪽으로 부는 바람이 이 열(col)에서 마지막으로 닿는 칸. 고지 벽과 유닛 벽 중 더 남쪽(가까운) 쪽을 기준으로 삼는다.
    private static Vector2Int FindNorthUnshelteredCell(WindShelterData terrain, UnitShelter units, RectInt playRect, int col)
    {
        bool hasHigh = terrain.TryGetColBottom(col, out int highY);
        bool hasUnit = units.TryGetColBottom(col, out int unitY);
        int high = ValueOrFallback(hasHigh, highY, playRect.yMax);
        int unit = ValueOrFallback(hasUnit, unitY, playRect.yMax);
        int wallY = Mathf.Min(high, unit);
        return new Vector2Int(col, wallY - 1);
    }

    // 남쪽으로 부는 바람이 이 열(col)에서 마지막으로 닿는 칸. 고지 벽과 유닛 벽 중 더 북쪽(가까운) 쪽을 기준으로 삼는다.
    private static Vector2Int FindSouthUnshelteredCell(WindShelterData terrain, UnitShelter units, RectInt playRect, int col)
    {
        bool hasHigh = terrain.TryGetColTop(col, out int highY);
        bool hasUnit = units.TryGetColTop(col, out int unitY);
        int high = ValueOrFallback(hasHigh, highY, playRect.yMin - 1);
        int unit = ValueOrFallback(hasUnit, unitY, playRect.yMin - 1);
        int wallY = Mathf.Max(high, unit);
        return new Vector2Int(col, wallY + 1);
    }
}
