 using UnityEngine;
using System;
using System.Collections.Generic;



public class EnemyPathView : MonoBehaviour
{
    [SerializeField] private MapBoard board;
    [SerializeField] private bool pathVisible = true;

    private readonly List<Tile> pathTiles = new();            // 적 레인 타일
    private readonly HashSet<Vector2Int> pathCoords = new();  // 레인 포함 여부 조회용

    public bool PathVisible => pathVisible;
    public bool HasPath => pathTiles.Count > 0;
    public int TileCount => pathTiles.Count;
    public IReadOnlyList<Tile> PathTiles => pathTiles;
    public bool IsOnPath(Vector2Int coord) => pathCoords.Contains(coord);

    public event Action OnPathChanged;

    private void Start()
    {
        RebuildPath();
    }

    public void RebuildPath()
    {
        pathTiles.Clear();
        pathCoords.Clear();

        if (board == null)
        {
            OnPathChanged?.Invoke();
            return;
        }

        List<Tile> newPath = board.GetPath();
        if (newPath != null)                      
        {
            foreach (Tile tile in newPath)
            {
                if (tile.IsEnemyLane)               
                {
                    pathTiles.Add(tile);        // 적 레인 타일만 추가
                    pathCoords.Add(tile.Coord); // 좌표 포함 여부 조회용
                }
            }
        }

        OnPathChanged?.Invoke();
    }

    public void ToggleVisibility()
    {
        pathVisible = !pathVisible;
        OnPathChanged?.Invoke();
    }
}