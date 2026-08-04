using System;
using System.Collections.Generic;
using UnityEngine;

public class LaneBuilder : ILaneBuilder
{
    private readonly RouteConfig routes;

    // 저작 경로 보관처를 받습니다. 없으면 항상 최단 경로로 계산합니다.
    public LaneBuilder(RouteConfig config = null)
    {
        routes = config;
    }

    // 모든 스폰을 좌표순으로 정렬하고 스폰마다 독립된 레인을 만듭니다.
    public IReadOnlyList<LaneData> BuildLanes(LaneInputData input)
    {
        var lanes = new List<LaneData>();
        var spawns = new List<Tile>(input.Spawns);
        var cores = new HashSet<Tile>(input.Cores);

        spawns.Sort(CompareSpawn);

        for (int i = 0; i < spawns.Count; i++)
        {
            AddSpawnLanes(input, spawns[i], cores, lanes);
        }

        return lanes;
    }

    // 이 스폰에 지정된 경로마다 레인 하나를 만듭니다. 지정이 없으면 자동 최단 경로 하나만 냅니다.
    private void AddSpawnLanes(LaneInputData input, Tile spawn, HashSet<Tile> cores, List<LaneData> lanes)
    {
        List<RouteData> spawnRoutes = GetRoutes(spawn);

        if (spawnRoutes.Count == 0)
        {
            lanes.Add(BuildLane(input, spawn, null, cores));
            return;
        }

        for (int index = 0; index < spawnRoutes.Count; index++)
        {
            lanes.Add(BuildLane(input, spawn, spawnRoutes[index], cores));
        }
    }

    // 레인 하나를 만듭니다. 계산에 실패하면 빈 레인을 냅니다.
    private LaneData BuildLane(LaneInputData input, Tile spawn, RouteData route, HashSet<Tile> cores)
    {
        if (cores.Count == 0)
        {
            return new LaneData(spawn, null, route, Array.Empty<Tile>());
        }

        List<Tile> path = FindRoute(input, spawn, route, cores);

        if (path == null || path.Count == 0)
        {
            return new LaneData(spawn, null, route, Array.Empty<Tile>());
        }

        Tile goal = path[path.Count - 1];
        return new LaneData(spawn, goal, route, path);
    }

    // 지정 경로가 있으면 그 노드를 따르고, 없으면 최단 경로를 씁니다.
    private List<Tile> FindRoute(LaneInputData input, Tile spawn, RouteData route, HashSet<Tile> cores)
    {
        if (route == null)
        {
            return AutoPath(input, spawn, cores);
        }

        List<Tile> nodes = GetNodes(input.Cells, route.Nodes);

        if (nodes == null)
        {
            return null;
        }

        return NodePath(input, spawn, nodes, cores);
    }

    // 이 스폰에 지정된 경로들. 보관처가 없거나 지정이 없으면 빈 목록입니다.
    private List<RouteData> GetRoutes(Tile spawn)
    {
        if (routes == null)
        {
            return EmptyRoutes;
        }

        if (routes.TryGetRoutes(spawn.Coord, out List<RouteData> found))
        {
            return found;
        }

        return EmptyRoutes;
    }

    private static readonly List<RouteData> EmptyRoutes = new();

    // 저작 좌표를 지나갈 수 있는 타일로 바꿉니다. 하나라도 어긋나면 실패합니다.
    private static List<Tile> GetNodes(
        IReadOnlyDictionary<Vector2Int, Tile> cells,
        IReadOnlyList<RouteNode> coords)
    {
        var nodes = new List<Tile>(coords.Count);

        for (int i = 0; i < coords.Count; i++)
        {
            bool found = cells.TryGetValue(coords[i].Coord, out Tile tile);

            if (!found || !tile.CanWalk)
            {
                return null;
            }

            nodes.Add(tile);
        }

        return nodes;
    }

    // 스폰에서 노드를 차례로 거쳐 코어까지 이어진 경로를 만듭니다.
    private static List<Tile> NodePath(
        LaneInputData input,
        Tile spawn,
        IReadOnlyList<Tile> nodes,
        HashSet<Tile> cores)
    {
        var path = new List<Tile> { spawn };
        Tile current = spawn;

        for (int i = 0; i < nodes.Count; i++)
        {
            List<Tile> segment = NodeSegment(input, current, nodes[i]);

            if (segment == null)
            {
                return null;
            }

            Append(path, segment);
            current = nodes[i];
        }

        List<Tile> last = AutoPath(input, current, cores);

        if (last == null)
        {
            return null;
        }

        Append(path, last);
        return path;
    }

    // 한 지점에서 지정된 노드까지의 최단 구간을 계산합니다.
    private static List<Tile> NodeSegment(LaneInputData input, Tile from, Tile node)
    {
        return Pathfinder.FindPath(
            new[] { from },
            tile => tile == node,
            CanWalk,
            tile => GridCalculator.GetDistance(tile.Coord, node.Coord));
    }

    // 코어까지의 최단 경로를 계산합니다.
    private static List<Tile> AutoPath(LaneInputData input, Tile from, HashSet<Tile> cores)
    {
        return Pathfinder.FindPath(
            new[] { from },
            tile => cores.Contains(tile),
            CanWalk,
            tile => GetDistance(tile, cores));
    }

    // 구간을 이어붙입니다. 첫 타일은 앞 구간의 끝과 겹치므로 건너뜁니다.
    private static void Append(List<Tile> path, List<Tile> segment)
    {
        for (int i = 1; i < segment.Count; i++)
        {
            path.Add(segment[i]);
        }
    }

    // 적이 지나갈 수 있는 칸인지 판단합니다. 걸어서 오는 적 기준이라 헤엄 칸은 통로에서 빠집니다.
    private static bool CanWalk(Tile tile) => tile.CanWalk;

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
