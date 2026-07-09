using System.Collections.Generic;
using UnityEngine;

public static class PathFinder
{
    private static bool missingTrack;
    static readonly Vector2Int[] Dirs =
    {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1)
    };

    // public static List<Vector2Int> FindPathPartial(/*TileMap map ,*/ Vector2Int start, Vector2Int goal, out bool reachedGoal)
    // {
    //     reachedGoal = false;

    //     if (!map.IsWalkable(start.x, start.y)) return null;

    //     var open = new List<Vector2Int> { start };
    //     var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
    //     var gScore = new Dictionary<Vector2Int, int> { [start] = 0 };
    //     var fScore = new Dictionary<Vector2Int, int> { [start] = Heuristic(start, goal) };
    //     var best = start;
    //     int bestH = Heuristic(start, goal);

    //     while (open.Count > 0)
    //     {
    //         int bestIdx = 0;
    //         for (int i = 1; i < open.Count; i++)
    //             if (fScore[open[i]] < fScore[open[bestIdx]]) bestIdx = i;

    //         var current = open[bestIdx];
    //         if (current == goal) { reachedGoal = true; return Reconstruct(cameFrom, current); }

    //         int h = Heuristic(current, goal);
    //         if (h < bestH) { bestH = h; best = current; }

    //         open.RemoveAt(bestIdx);

    //         foreach (var d in Dirs)
    //         {
    //             var nb = current + d;
    //             if (!map.IsWalkable(nb.x, nb.y)) continue;

    //             int tentative = gScore[current] + 1;
    //             if (!gScore.TryGetValue(nb, out int gNb) || tentative < gNb)
    //             {
    //                 cameFrom[nb] = current;
    //                 gScore[nb] = tentative;
    //                 fScore[nb] = tentative + Heuristic(nb, goal);
    //                 if (!open.Contains(nb)) open.Add(nb);
    //             }
    //         }
    //     }
    //     return Reconstruct(cameFrom, best);
    // }
       static int Heuristic(Vector2Int a, Vector2Int b)
        => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    static List<Vector2Int> Reconstruct(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        var path = new List<Vector2Int> { current };
        while (cameFrom.TryGetValue(current, out var prev))
        {
            current = prev;
            path.Add(current);
        }
        path.Reverse();
        return path;
    }
}
