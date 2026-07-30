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

    // HeroSkillCastController는 일반 C# 클래스(비-MonoBehaviour)라 인스펙터로 연결할 수 없다 —
    // MapAssemble이 조립 시점에 코드로 넣어준다.
    public HeroSkillCastController skillCast;

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
            PaintPath(module.GetComponent<EnemyPathView>());
            //PaintState(board);
        }

        PaintHover();
        PaintSkillRange();
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

        if (game.IsPlacing)
        {
            PaintPlace(game.HoverArea);
            return;
        }

        Tile tile = game.HoverTile;
        if (tile == null)
        {
            return;
        }

        if (tile.OccupantObject != null)
        {
            int range = game.UnitRange(tile.OccupantObject);
            if (range >= 0)
            {
                //PaintRange(tile, range);
            }
        }
    }

    private void PaintPlace(PlacementArea area)
    {
        if (area == null)
        {
            return;
        }

        OccupantKind kind = game.PlacingKind;

        // 한 칸이라도 막히면 덮는 칸 전체를 거부색으로 칠한다(부분 배치가 없으므로 색도 부분이면 안 된다).
        // 판정은 자리가 속한 모듈 보드 기준이라 어느 모듈에서든 프리뷰가 맞게 뜬다.
        Color color = AreaPlace.CanPlace(area, kind) ? painter.okColor : painter.denyColor;

        foreach (Vector2Int cell in area.Cells)
        {
            if (area.Board.TryGetCell(cell, out Tile tile))
            {
                Paint(tile, color);
            }
        }
    }

    // 영웅을 클릭해 스킬 시전자로 선택된 상태일 때, 스킬이 "실제로 때리는 범위"를 미리 보여준다.
    // (targetScope는 "어디를 클릭할 수 있는가"일 뿐, 여기서 칠하는 건 그 지점 중심의 타격 범위다.)
    private void PaintSkillRange()
    {
        if (skillCast == null) return;
        Hero caster = skillCast.SelectedCaster;
        if (caster == null || caster.ActiveSkill == null) return;

        HeroActiveSkillDataSO skill = caster.ActiveSkill;
        int hitRadius = skill.groundZone != null ? skill.groundZone.radius : 0;
        RangeShape hitShape = skill.groundZone != null ? skill.groundZone.shape : RangeShape.Diamond;

        Tile origin;
        if (skill.targetScope == SkillTargetScope.Self)
        {
            origin = caster.CurrentTile;
        }
        else
        {
            origin = game != null ? game.HoverTile : null;
            if (origin != null && origin.Board != caster.Board) origin = null;
        }
        if (origin == null) return;

        foreach (Tile tile in TileShapeQuery.GetTiles(caster.Board, origin.Coord, hitRadius, hitShape))
            Paint(tile, painter.rangeColor);
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
