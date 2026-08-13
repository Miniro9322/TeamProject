using UnityEngine;

// 고정된 언덕 가림막을 읽어 바람 방향별 보호 칸을 맵 로드 때 한 번 계산합니다.
public class WindwallCalc
{
    // 안쪽 칸을 한 번 훑어 가림막마다 네 방향 팔을 채운 결과를 돌려줍니다.
    public WindwallData BuildData(MapBoard board, int reach)
    {
        WindwallData data = new();
        RectInt play = board.PlayRect;

        for (int row = play.yMin; row < play.yMax; row++)
        {
            KeepRowArms(board, data, play, row, reach);
        }

        return data;
    }

    // 한 행을 훑어 가림막 칸마다 네 방향 팔을 채운다.
    private static void KeepRowArms(MapBoard board, WindwallData data, RectInt play, int row, int reach)
    {
        for (int col = play.xMin; col < play.xMax; col++)
        {
            Vector2Int cell = new(col, row);
            if (IsHighWindwall(board, cell))
            {
                KeepArms(data, cell, reach);
            }
        }
    }

    // 이 좌표에 언덕 위 가림막이 있는지 확인한다. 지상은 고지가 대신 막아주므로 가림막으로 인정하지 않는다.
    private static bool IsHighWindwall(MapBoard board, Vector2Int cell)
    {
        if (!board.TryGetCell(cell, out Tile tile))
        {
            return false;
        }

        if (!tile.IsHigh)
        {
            return false;
        }

        return tile.IsWindwall;
    }

    // 가림막 한 칸에서 네 방향으로 reach 칸까지 보호 칸을 보관한다.
    private static void KeepArms(WindwallData data, Vector2Int origin, int reach)
    {
        for (int index = 0; index < GridCalculator.Directions.Length; index++)
        {
            Vector2Int wind = GridCalculator.Directions[index];
            for (int step = 1; step <= reach; step++)
            {
                data.KeepArm(wind, origin + wind * step);
            }
        }
    }
}
