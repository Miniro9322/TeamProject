using System.Collections.Generic;
using UnityEngine;

// 고정된 언덕 가림막을 읽어 바람 방향별 보호 칸을 맵 로드 때 한 번 계산합니다.
// 칸 딕셔너리만 있으면 계산 가능하다 — 런타임(MapBoard.Cells)과 저작 창(에디터 스캔) 둘 다 같은 함수를 쓴다.
public class WindwallCalc
{
    // 안쪽 칸을 한 번 훑어 가림막마다 네 방향 팔을 채운 결과를 돌려줍니다.
    public WindwallData BuildData(IReadOnlyDictionary<Vector2Int, Tile> cells, int reach)
    {
        WindwallData data = new();

        foreach (Tile tile in cells.Values)
        {
            if (IsHighWindwall(tile))
            {
                KeepArms(cells, data, tile.Coord, reach);
            }
        }

        return data;
    }

    // 이 타일이 언덕 위 가림막인지 확인한다. 지상은 고지가 대신 막아주므로 가림막으로 인정하지 않는다.
    private static bool IsHighWindwall(Tile tile)
    {
        if (!tile.IsHigh)
        {
            return false;
        }

        return tile.IsWindwall;
    }

    // 가림막 한 칸에서 네 방향 팔을 보관하고, 이 가림막 혼자만의 범위도 함께 모은다.
    private static void KeepArms(IReadOnlyDictionary<Vector2Int, Tile> cells, WindwallData data, Vector2Int origin, int reach)
    {
        List<Tile> range = new();
        for (int index = 0; index < GridCalculator.Directions.Length; index++)
        {
            Vector2Int wind = GridCalculator.Directions[index];
            KeepArm(cells, data, origin, wind, reach, range);
        }

        data.KeepRange(origin, range);
    }

    // 한 방향으로 reach 칸까지 보호 칸을 보관하고, 실제 타일이 있는 칸은 범위 목록에도 담는다.
    private static void KeepArm(
        IReadOnlyDictionary<Vector2Int, Tile> cells,
        WindwallData data,
        Vector2Int origin,
        Vector2Int wind,
        int reach,
        List<Tile> range)
    {
        for (int step = 1; step <= reach; step++)
        {
            Vector2Int cell = origin + wind * step;
            data.KeepArm(wind, cell);
            if (cells.TryGetValue(cell, out Tile tile))
            {
                range.Add(tile);
            }
        }
    }
}
