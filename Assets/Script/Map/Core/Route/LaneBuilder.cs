using System.Collections.Generic;
using UnityEngine;

public class LaneBuilder : ILaneBuilder
{
    // 모든 스폰을 좌표순으로 정렬하고 스폰마다 독립된 레인을 만듭니다.
    public IReadOnlyList<LaneData> BuildLanes(LaneInputData input)
    {
        var lanes = new List<LaneData>();
        var spawns = new List<Tile>(input.Spawns);
        var cores = new HashSet<Tile>(input.Cores);

        spawns.Sort(CompareSpawn);

        for (int i = 0; i < spawns.Count; i++)
        {
            lanes.Add(BuildLane(input, spawns[i], cores));
        }

        return lanes;
    }

    // 스폰 하나에서 도달 가능한 코어까지의 최단 레인을 계산합니다.
    private static LaneData BuildLane(LaneInputData input, Tile spawn, HashSet<Tile> cores)
    {
        if (cores.Count == 0)
        {
            return new LaneData(spawn, null, null);
        }

        List<Tile> path = Pathfinder.FindPath(
            new[] { spawn },
            tile => cores.Contains(tile),
            tile => GetNeighbors(input.Cells, tile),
            tile => GetDistance(tile, cores));

        if (path == null || path.Count == 0)
        {
            return new LaneData(spawn, null, null);
        }

        Tile goal = path[path.Count - 1];
        return new LaneData(spawn, goal, path);
    }

    // 현재 타일의 상하좌우에서 이동 가능한 이웃 타일만 반환합니다.
    private static IEnumerable<Tile> GetNeighbors(
        IReadOnlyDictionary<Vector2Int, Tile> cells,
        Tile tile)
    {
        foreach (Vector2Int step in GridCalculator.Directions)
        {
            bool found = cells.TryGetValue(tile.Coord + step, out Tile side);
            if (found && side.Walkable)
            {
                yield return side;
            }
        }
    }

    // 현재 타일에서 가장 가까운 코어까지의 맨해튼 거리를 구합니다.
    private static int GetDistance(Tile tile, HashSet<Tile> cores)
    {
        int nearest = int.MaxValue;

        foreach (Tile core in cores)
        {
            int gap = GridCalculator.GetDistance(tile.Coord, core.Coord);
            if (gap < nearest)
            {
                nearest = gap;
            }
        }

        return nearest;
    }

    // 스폰을 X 좌표 우선, Y 좌표 차순으로 비교합니다.
    private static int CompareSpawn(Tile left, Tile right)
    {
        int column = left.Coord.x.CompareTo(right.Coord.x);
        if (column != 0)
        {
            return column;
        }

        return left.Coord.y.CompareTo(right.Coord.y);
    }
}
