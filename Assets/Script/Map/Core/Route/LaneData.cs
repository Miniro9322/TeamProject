using System.Collections.Generic;
using UnityEngine;

public class LaneData
{
    private readonly List<Tile> tiles;

    public Tile Start { get; }
    public Tile Goal { get; }
    public IReadOnlyList<Tile> Tiles => tiles;
    public bool IsValid => Goal != null && tiles.Count > 0;

    public LaneData(Tile start, Tile goal, IReadOnlyList<Tile> source)
    {
        Start = start;
        Goal = goal;
        tiles = new List<Tile>();
        
        for (int i = 0; i < source.Count; i++)
        {
            tiles.Add(source[i]);
        }
    }

    public List<Vector3> GetPoints(float yOffset)
    {
        var points = new List<Vector3>(tiles.Count);
        Vector3 lift = Vector3.up * yOffset;

        for (int i = 0; i < tiles.Count; i++)
        {
            points.Add(tiles[i].WorldTop + lift);
        }

        return points;
    }
}
