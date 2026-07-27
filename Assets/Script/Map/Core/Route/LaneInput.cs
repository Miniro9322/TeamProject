using System.Collections.Generic;
using UnityEngine;

public class LaneInput
{
    private readonly Dictionary<Vector2Int, Tile> cells;
    private readonly List<Tile> spawns;
    private readonly List<Tile> cores;

    public IReadOnlyDictionary<Vector2Int, Tile> Cells => cells;
    public IReadOnlyList<Tile> Spawns => spawns;
    public IReadOnlyList<Tile> Cores => cores;

    public LaneInput(
        IReadOnlyDictionary<Vector2Int, Tile> source,
        IReadOnlyList<Tile> starts,
        IReadOnlyList<Tile> goals)
    {
        cells = new Dictionary<Vector2Int, Tile>();
        spawns = new List<Tile>();
        cores = new List<Tile>();

        CopyCells(source);
        CopyTiles(starts, spawns);
        CopyTiles(goals, cores);
    }

    private void CopyCells(IReadOnlyDictionary<Vector2Int, Tile> source)
    {
        foreach (KeyValuePair<Vector2Int, Tile> pair in source)
        {
            cells[pair.Key] = pair.Value;
        }
    }

    private static void CopyTiles(IReadOnlyList<Tile> source, List<Tile> target)
    {
        for (int i = 0; i < source.Count; i++)
        {
            target.Add(source[i]);
        }
    }
}
