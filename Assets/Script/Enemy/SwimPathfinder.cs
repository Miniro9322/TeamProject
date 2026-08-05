using System.Collections.Generic;
using UnityEngine;

// 수영(헤엄 칸 통행) 격자 길찾기.
// 지상 길찾기와 동일한 A*(Pathfinder)지만 통행 판정을 CanPass(PassType.Swim)로 바꿔
// 걷는 적이 못 지나는 물 칸(State.Pass == Swim)까지 길로 인정한다. 고지·빈 칸은 여전히 막힌다.
// MapBoard는 공개 API(TryGetCell/WorldToCell)만 읽고 절대 수정하지 않는다(맵은 다른 담당 영역).
public static class SwimPathfinder
{
    /// <summary>startWorld→본진을 잇는 격자 경로(웨이포인트). 경로 없으면 빈 리스트.
    /// 물 위도 걸어서 건너는 연출이라 Y는 타일 윗면(WorldTop) 그대로 쓴다.</summary>
    public static List<Vector3> BuildWaypoints(MapBoard board, Vector3 startWorld, Vector3 goalWorld)
    {
        var list = new List<Vector3>();
        if (board == null) return list;

        if (!board.TryGetCell(board.WorldToCell(startWorld), out Tile startTile)) return list;

        // 도착 판정은 좌표 비교가 아니라 IsCore로 한다 — 타일 좌표는 모듈 로컬 0-base라
        // 보드가 여러 개면 다른 보드의 같은 좌표에 잘못 걸린다(Tile.Board 주석 참조).
        // goalCell은 휴리스틱(탐색 방향 힌트)에만 쓰므로 근사값이어도 경로 정확도에는 영향이 없다.
        Vector2Int goalCell = board.WorldToCell(goalWorld);
        List<Tile> path = Pathfinder.FindPath(
            new[] { startTile },
            tile => tile.IsCore,
            PassSwim,
            tile => GridCalculator.GetDistance(tile.Coord, goalCell));

        if (path == null) return list;
        foreach (Tile t in path)
        {
            list.Add(t.WorldTop);
        }
        return list;
    }

    // 헤엄치는 적 기준 통행 판정. Tile.CanPass가 물 칸을 열어 주고, 고지·빈 칸은 Walkable에서 걸러진다.
    private static bool PassSwim(Tile tile)
    {
        return tile.CanPass(PassType.Swim);
    }
}
