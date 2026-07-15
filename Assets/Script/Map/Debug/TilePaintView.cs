using UnityEngine;
using System.Collections.Generic;

/// <summary>타일 색칠(viz) 전담 — 검증용, 게임 로직 아님. 매 프레임 지우고 다시 그린다.</summary>
public class TilePaintView : MonoBehaviour
{
    [SerializeField] private MapBoard board;
    [SerializeField] private EnemyPathView enemyPath; // 경로 데이터
    [SerializeField] private MapGame game;            // 호버·배치 상태(읽기 전용)
    [SerializeField] private TilePainter painter;
    [SerializeField] private bool showEnemyTiles = true;
    [SerializeField] private bool showBlocking = true;

    private readonly List<Vector2Int> cellPainted = new(); // 이번 프레임 칠한 칸

    private void Update()
    {
        if (board == null || painter == null)
        {
            return;
        }

        RestoreCells();
        PaintPath();
        PaintState();
        PaintHover();
    }

    private void RestoreCells()
    {
        for (int i = 0; i < cellPainted.Count; i++)
        {
            painter.ClearColor(cellPainted[i]);
        }
        cellPainted.Clear();
    }

    private void PaintPath()
    {
        if (enemyPath == null || !enemyPath.PathVisible)
        {
            return;
        }

        foreach (Tile tile in enemyPath.PathTiles)
        {
            Paint(tile.Coord, painter.pathColor);
        }
    }

    private void PaintState()
    {
        foreach (Tile tile in board.Cells.Values)
        {
            if (showBlocking && !tile.IsEmpty && tile.HasEnemy)
            {
                Paint(tile.Coord, painter.blockColor);
            }
            else if (showEnemyTiles && tile.HasEnemy)
            {
                Paint(tile.Coord, painter.enemyColor);
            }
        }
    }

    private void PaintHover()
    {
        if (game == null || game.InputBlocked)
        {
            return;
        }

        Tile tile = game.HoverTile;
        if (tile == null)
        {
            return;
        }

        if (game.IsPlacing)
        {
            PaintPlace(tile);
        }
        else if (tile.OccupantObject != null)
        {
            int range = game.UnitRange(tile.OccupantObject, tile.State.Occupant);
            if (range >= 0)
            {
                PaintRange(tile.Coord, range);
            }
        }
    }

    private void PaintPlace(Tile tile)
    {
        OccupantKind kind = game.PlacingKind;
        if (kind == OccupantKind.MeleeHero || kind == OccupantKind.RangedHero)
        {
            PaintRange(tile.Coord, game.PlacingRange);
        }

        bool ok = board.CanPlace(tile.Coord, kind, out _);
        Paint(tile.Coord, ok ? painter.okColor : painter.denyColor);
    }

    private void PaintRange(Vector2Int center, int range)
    {
        foreach (Tile tile in board.GetTiles(center, range, false))
        {
            if (tile.Coord != center)
            {
                Paint(tile.Coord, painter.rangeColor);
            }
        }
    }

    private void Paint(Vector2Int coord, Color color)
    {
        painter.SetColor(coord, color);
        cellPainted.Add(coord);
    }
}
