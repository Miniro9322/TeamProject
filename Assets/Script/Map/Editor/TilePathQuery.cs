using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 좌표 격자에서 스폰→본진 경로를 계산하는 에디터 전용 도구.
/// 넘겨받은 사전만 읽고 어떤 공유 상태도 건드리지 않는다 — MapBoard.GetPath와 달리
/// EnemyLane을 켜지 않는다(저작 중 미리보기가 게임 상태를 바꾸면 안 되므로).
///
/// 탐색 조건은 MapBoard와 똑같이 맞춘다(A* + 가장 가까운 본진까지의 맨해튼 휴리스틱).
/// 최단 경로가 여럿일 때 게임이 고르는 것과 같은 길이 보여야 미리보기가 뜻을 가진다.
/// </summary>
public static class TilePathQuery
{
    /// <summary>스폰에서 가장 가까운 본진까지의 경로. 스폰·본진이 없거나 막혔으면 null.</summary>
    public static List<Tile> FindPath(Dictionary<Vector2Int, Tile> cells)
    {
        List<Tile> spawns = CollectSpawns(cells);
        List<Tile> cores = CollectCores(cells);

        if (spawns.Count == 0)
        {
            return null;
        }

        if (cores.Count == 0)
        {
            return null;
        }

        IEnumerable<Tile> Neighbors(Tile tile)
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

        int Heuristic(Tile tile)
        {
            return DistanceToNearestCore(tile, cores);
        }

        return Pathfinder.FindPath(spawns, IsCore, Neighbors, Heuristic);
    }

    /// <summary>적 스폰으로 찍힌 타일들(경로 시작점).</summary>
    public static List<Tile> CollectSpawns(Dictionary<Vector2Int, Tile> cells)
    {
        var spawns = new List<Tile>();
        foreach (Tile tile in cells.Values)
        {
            if (tile.IsEnemySpawn)
            {
                spawns.Add(tile);
            }
        }

        return spawns;
    }

    /// <summary>본진(Terrain=Core)으로 찍힌 타일들(경로 도착점).</summary>
    public static List<Tile> CollectCores(Dictionary<Vector2Int, Tile> cells)
    {
        var cores = new List<Tile>();
        foreach (Tile tile in cells.Values)
        {
            if (tile.IsCore)
            {
                cores.Add(tile);
            }
        }

        return cores;
    }

    private static bool IsCore(Tile tile)
    {
        return tile.IsCore;
    }

    // 4방향 균일비용에서 맨해튼 거리는 admissible → 최단 경로가 보장된다(MapBoard와 동일).
    private static int DistanceToNearestCore(Tile tile, List<Tile> cores)
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
}
