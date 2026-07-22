using UnityEngine;
using System.Collections.Generic;

/// <summary>타일 색칠(viz) 전담 — 검증용, 게임 로직 아님. 매 프레임 지우고 다시 그린다.</summary>
public class TilePaintView : MonoBehaviour
{
    [SerializeField] private MapRegistry registry;
    [SerializeField] private MapView game;            // 호버·배치 상태(읽기 전용)
    [SerializeField] private TilePainter painter;
    [SerializeField] private bool showEnemyTiles = true;
    [SerializeField] private bool showBlocking = true;

    private readonly List<Tile> cellPainted = new();

    private void Update()
    {
        if (registry == null || painter == null)
        {
            return;
        }

        RestoreCells();

        foreach (ModuleLogic module in registry.AllModules.Values)
        {
            MapBoard board = module.GetComponent<MapBoard>();
            if (board == null)
            {
                continue;
            }

            PaintPath(module.GetComponent<EnemyPathView>());
            PaintState(board);
        }

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

    private void PaintPath(EnemyPathView pathView)
    {
        if (pathView == null || !pathView.PathVisible)
        {
            return;
        }

        foreach (Tile tile in pathView.PathTiles)
        {
            Paint(tile, painter.pathColor);
        }
    }

    private void PaintState(MapBoard board)
    {
        foreach (Tile tile in board.Cells.Values)
        {
            if (showBlocking && tile.HasUnit && tile.HasEnemy)
            {
                Paint(tile, painter.blockColor);
            }
            else if (showEnemyTiles && tile.HasEnemy)
            {
                Paint(tile, painter.enemyColor);
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
            int range = game.UnitRange(tile.OccupantObject);
            if (range >= 0)
            {
                // PaintRange(tile, range);
            }
        }
    }

    private void PaintPlace(Tile tile)
    {
        OccupantKind kind = game.PlacingKind;
        if (kind == OccupantKind.MeleeHero || kind == OccupantKind.RangedHero)
        {
            // PaintRange(tile, game.PlacingRange);
        }

        // 호버된 타일이 속한 모듈 보드 기준으로 판정한다(어느 모듈이든 프리뷰가 맞게 뜬다).
        if (tile.Board.CanPlace(tile.Coord, kind, out _))
        {
            Paint(tile, painter.okColor);
        }
        else
        {
            Paint(tile, painter.denyColor);
        }
    }

    private void PaintRange(Tile center, int range)
    {
        foreach (Tile tile in center.Board.GetTiles(center.Coord, range, false))
        {
            if (tile != center)
            {
                Paint(tile, painter.rangeColor);
            }
        }
    }

    private void Paint(Tile tile, Color color)
    {
        painter.SetColor(tile, color);
        cellPainted.Add(tile);
    }
}
