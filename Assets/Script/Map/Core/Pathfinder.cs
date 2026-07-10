using System.Collections.Generic;
using System;

 //탐색 알고리즘(임시용)
public static class Pathfinder
{
 
    public static List<T> FindPath<T>(
        IEnumerable<T> sources,
        Func<T, bool> isGoal,
        Func<T, IEnumerable<T>> getNeighbors,
        Func<T, int> heuristic = null)
    {
        return heuristic == null
            ? Bfs(sources, isGoal, getNeighbors)
            : AStar(sources, isGoal, getNeighbors, heuristic);
    }

    private static List<T> Bfs<T>(
        IEnumerable<T> sources, Func<T, bool> isGoal, Func<T, IEnumerable<T>> getNeighbors)
    {
        var cameFrom = new Dictionary<T, T>();
        var visited = new HashSet<T>();
        var queue = new Queue<T>();

        foreach (T s in sources)
        {
            if (!visited.Add(s)) continue;
            if (isGoal(s)) return new List<T> { s };
            queue.Enqueue(s);
        }

        while (queue.Count > 0)
        {
            T current = queue.Dequeue();
            foreach (T next in getNeighbors(current))
            {
                if (!visited.Add(next)) continue;
                cameFrom[next] = current;
                if (isGoal(next)) return Reconstruct(cameFrom, next);
                queue.Enqueue(next);
            }
        }

        return null;
    }
    private static List<T> AStar<T>(
        IEnumerable<T> sources, Func<T, bool> isGoal, Func<T, IEnumerable<T>> getNeighbors, Func<T, int> heuristic)
    {
        var cameFrom = new Dictionary<T, T>();
        var gScore = new Dictionary<T, int>();
        var open = new List<T>();

        foreach (T s in sources)
        {
            if (gScore.ContainsKey(s)) continue;
            if (isGoal(s)) return new List<T> { s };
            gScore[s] = 0;
            open.Add(s);
        }

        while (open.Count > 0)
        {
            
            int best = 0;
            int bestF = gScore[open[0]] + heuristic(open[0]);
            for (int i = 1; i < open.Count; i++)
            {
                int f = gScore[open[i]] + heuristic(open[i]);
                if (f < bestF) { bestF = f; best = i; }
            }

            T current = open[best];
            if (isGoal(current)) return Reconstruct(cameFrom, current);
            open.RemoveAt(best);

            int g = gScore[current];
            foreach (T next in getNeighbors(current))
            {
                int tentative = g + 1;
                if (gScore.TryGetValue(next, out int known) && tentative >= known) continue;

                cameFrom[next] = current;
                gScore[next] = tentative;
                if (!open.Contains(next)) open.Add(next);
            }
        }

        return null;
    }

   
    public static int ReachableCount<T>(IEnumerable<T> sources, Func<T, IEnumerable<T>> getNeighbors)
    {
        var visited = new HashSet<T>();
        var queue = new Queue<T>();
        foreach (T s in sources) if (visited.Add(s)) queue.Enqueue(s);

        while (queue.Count > 0)
        {
            T current = queue.Dequeue();
            foreach (T next in getNeighbors(current))
                if (visited.Add(next)) queue.Enqueue(next);
        }

        return visited.Count;
    }

    private static List<T> Reconstruct<T>(Dictionary<T, T> cameFrom, T goal)
    {
        var path = new List<T> { goal };
        T current = goal;
        while (cameFrom.TryGetValue(current, out T prev))
        {
            current = prev;
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}
