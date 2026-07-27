using System.Collections.Generic;
using UnityEngine;

public class LaneBuilder : ILaneBuilder
{
    public IReadOnlyList<LaneData> BuildLanes(LaneInput input)
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

    private static LaneData BuildLane(LaneInput input, Tile spawn, HashSet<Tile> cores)
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
