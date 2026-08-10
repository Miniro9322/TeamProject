using System;
using UnityEngine;

public static class WindDirectionText
{
    public static string ReadDirection(Vector2Int windDirection)
    {
        if (windDirection == GridCalculator.Down) return "북쪽 → 남쪽";
        if (windDirection == GridCalculator.Up) return "남쪽 → 북쪽";
        if (windDirection == GridCalculator.Right) return "서쪽 → 동쪽";
        if (windDirection == GridCalculator.Left) return "동쪽 → 서쪽";

        throw new ArgumentOutOfRangeException(nameof(windDirection));
    }
}
