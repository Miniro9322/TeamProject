using System;
using UnityEngine;

public static class WindPicker
{
    // 현재 방향을 제외한 나머지 세 방향 중 하나를 무작위로 반환합니다.
    public static Vector2Int Next(Vector2Int current)
    {
        int currentIndex = ReadIndex(current);
        int offset = UnityEngine.Random.Range(1, GridCalculator.Directions.Length);
        int nextIndex = (currentIndex + offset) % GridCalculator.Directions.Length;
        return GridCalculator.Directions[nextIndex];
    }

    // 현재 방향이 네 방향 배열에서 몇 번째인지 조회합니다.
    private static int ReadIndex(Vector2Int current)
    {
        int index = Array.IndexOf(GridCalculator.Directions, current);
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(current));
        }

        return index;
    }
}
