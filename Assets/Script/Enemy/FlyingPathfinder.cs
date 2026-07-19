using System.Collections.Generic;
using UnityEngine;

// 공중(지형 무시) 격자 길찾기.
// 지상 길찾기와 동일한 A*(Pathfinder)지만 Walkable 제약을 빼서 벽·고지 위로 넘어간다.
// MapBoard는 공개 API(TryGetCell/WorldToCell)만 읽고 절대 수정하지 않는다(맵은 다른 담당 영역).
public static class FlyingPathfinder
{
    /// <summary>startWorld→goalWorld 를 잇는 격자 경로(웨이포인트). 경로 없으면 빈 리스트.
    /// 지형(언덕) 높이를 무시하고 시작 지점 높이 + flightHeight로 Y를 고정 — 공중 유닛이 오르내리지 않게.</summary>
    public static List<Vector3> BuildWaypoints(MapBoard board, Vector3 startWorld, Vector3 goalWorld, float flightHeight = 0f)
    {
        var list = new List<Vector3>();
        if (board == null) return list;

        Vector2Int goalCell = board.WorldToCell(goalWorld);
        if (!board.TryGetCell(board.WorldToCell(startWorld), out Tile startTile)) return list;

        // WalkableNeighbors 대신 AllNeighbors — 통행 지형 제약만 뺀 동일 A*. 목표는 본진 칸.
        List<Tile> path = Pathfinder.FindPath(
            new[] { startTile },
            t => t.Coord == goalCell,
            t => AllNeighbors(board, t),
            t => GridCalculator.GetDistance(t.Coord, goalCell));

        if (path == null) return list;

        // 모든 웨이포인트의 Y를 일정 고도로 고정(XZ만 타일 위치 사용) → 언덕 위에서도 평탄하게 비행.
        float flightY = startWorld.y + flightHeight;
        foreach (Tile t in path)
        {
            Vector3 top = t.WorldTop;
            list.Add(new Vector3(top.x, flightY, top.z));
        }
        return list;
    }

    // 그리드 내 모든 이웃(통행 지형 여부 무시).
    private static IEnumerable<Tile> AllNeighbors(MapBoard board, Tile tile)
    {
        foreach (Vector2Int dir in GridCalculator.Directions)
            if (board.TryGetCell(tile.Coord + dir, out Tile nb))
                yield return nb;
    }
}
