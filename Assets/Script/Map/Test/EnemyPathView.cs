using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class EnemyPathView : MonoBehaviour
{
    [FormerlySerializedAs("routeMap")]
    [SerializeField] private EnemyLanes enemyLanes;
    [SerializeField] private bool pathVisible = true;

    private readonly List<Tile> pathTiles = new();
    private readonly HashSet<Vector2Int> pathCoords = new();

    public bool PathVisible => pathVisible;
    public bool HasPath => pathTiles.Count > 0;
    public int TileCount => pathTiles.Count;
    public IReadOnlyList<Tile> PathTiles => pathTiles;

    public event Action OnPathChanged;

    private void Reset()
    {
        enemyLanes = GetComponent<EnemyLanes>();
    }

    private void OnEnable()
    {
        Prepare();
        if (enemyLanes != null)
        {
            enemyLanes.Changed += RebuildPath;
        }
    }

    private void Start()
    {
        RebuildPath();
    }

    private void OnDisable()
    {
        if (enemyLanes != null)
        {
            enemyLanes.Changed -= RebuildPath;
        }
    }

    public bool IsOnPath(Vector2Int coord)
    {
        return pathCoords.Contains(coord);
    }

    public void RebuildPath()
    {
        pathTiles.Clear();
        pathCoords.Clear();
        Prepare();

        if (enemyLanes == null)
        {
            OnPathChanged?.Invoke();
            return;
        }

        AddLanes(enemyLanes.Lanes);
        OnPathChanged?.Invoke();
    }

    public void ToggleVisibility()
    {
        pathVisible = !pathVisible;
        OnPathChanged?.Invoke();
    }

    private void Prepare()
    {
        if (enemyLanes == null)
        {
            enemyLanes = GetComponent<EnemyLanes>();
        }
    }

    private void AddLanes(IReadOnlyList<LaneData> lanes)
    {
        for (int i = 0; i < lanes.Count; i++)
        {
            AddLane(lanes[i]);
        }
    }

    private void AddLane(LaneData lane)
    {
        if (!lane.IsValid)
        {
            return;
        }

        for (int i = 0; i < lane.Tiles.Count; i++)
        {
            Tile tile = lane.Tiles[i];
            bool endpoint = tile.IsEnemySpawn || tile.IsCore;
            if (endpoint || !pathCoords.Add(tile.Coord))
            {
                continue;
            }

            pathTiles.Add(tile);
        }
    }
}
