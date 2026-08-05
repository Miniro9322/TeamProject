using System.Collections.Generic;
using System;

 //탐색 알고리즘(임시용)
public static class Pathfinder
{
    // 출발 칸까지 온 비용(아직 한 칸도 안 움직였다)
    private const int StartCost = 0;

    // 이웃 칸으로 한 칸 옮기는 비용
    private const int StepCost = 1;

    // 시작 칸에서 목표 칸까지 가장 짧은 길을 찾는다(A*). 이웃은 타일이 미리 이어 둔 목록에서 읽는다.
    public static List<Tile> FindPath(
        IEnumerable<Tile> sources,
        Func<Tile, bool> isGoal,
        Func<Tile, bool> canPass,
        Func<Tile, int> heuristic)
    {
        var cameFrom = new Dictionary<Tile, Tile>();
        var bestCost = new Dictionary<Tile, int>();
        var open = new List<Tile>();

        foreach (Tile source in sources)
        {
            if (bestCost.ContainsKey(source))
            {
                continue;
            }

            if (isGoal(source))
            {
                return new List<Tile> { source };
            }

            bestCost[source] = StartCost;
            open.Add(source);
        }

        while (open.Count > 0)
        {
            int pick = 0;
            int pickScore = bestCost[open[0]] + heuristic(open[0]);

            for (int index = 1; index < open.Count; index++)
            {
                int score = bestCost[open[index]] + heuristic(open[index]);
                if (score < pickScore)
                {
                    pickScore = score;
                    pick = index;
                }
            }

            Tile current = open[pick];
            if (isGoal(current))
            {
                return Reconstruct(cameFrom, current);
            }

            open.RemoveAt(pick);
            int reached = bestCost[current] + StepCost;

            foreach (Tile neighbor in current.NeighborTiles)
            {
                if (!canPass(neighbor))
                {
                    continue;
                }

                if (bestCost.TryGetValue(neighbor, out int known) && reached >= known)
                {
                    continue;
                }

                cameFrom[neighbor] = current;
                bestCost[neighbor] = reached;

                if (!open.Contains(neighbor))
                {
                    open.Add(neighbor);
                }
            }
        }

        return null;
    }

    // 도착 칸에서 온 길을 거꾸로 되짚어 스폰부터의 순서로 편다.
    private static List<Tile> Reconstruct(Dictionary<Tile, Tile> cameFrom, Tile goal)
    {
        var path = new List<Tile> { goal };
        Tile current = goal;

        while (cameFrom.TryGetValue(current, out Tile previous))
        {
            current = previous;
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}
