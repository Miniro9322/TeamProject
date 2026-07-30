using UnityEngine;
using System.Collections.Generic;

/// <summary>타일 표시를 전담하며 매 프레임 지우고 다시 그린다.</summary>
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
            PaintPath(module.GetComponent<EnemyPathView>());
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
    private void PaintHover()
    {
        if (game == null || game.InputBlocked)
        {
            return;
        }

        if (game.IsPlacing)
        {
            PaintPreview(
                game.HoverArea,
                game.PlacingPrefab,
                game.PlacingKind);
            return;
        }

        bool isHeld = game.IsReplacing && game.IsHolding;
        if (isHeld)
        {
            PaintPreview(
                game.HeldArea,
                game.HeldUnit,
                game.HeldKind);
            return;
        }

        Tile tile = game.HoverTile;
        if (tile == null)
        {
            return;
        }

        GameObject unit = tile.OccupantObject;
        bool hasUnit = unit != null;
        if (!hasUnit)
        {
            return;
        }

        bool hasRange = game.TryRange(
            unit,
            out int range,
            out RangeShape shape);

        if (!hasRange)
        {
            return;
        }

        PaintRange(
            tile,
            range,
            shape,
            painter.rangeColor);
    }

    private void PaintPreview(
        PlacementArea area,
        GameObject unit,
        OccupantKind kind)
    {
        if (area == null)
        {
            return;
        }

        bool canPlace = AreaPlace.CanPlace(area, kind);
        Color placeColor;
        Color rangeColor = painter.rangeColor;

        if (canPlace)
        {
            placeColor = painter.okColor;
        }
        else
        {
            placeColor = painter.denyColor;
            rangeColor = painter.denyColor;
        }

        bool hasCenter = area.Board.TryGetCell(
            area.Origin,
            out Tile center);

        if (hasCenter)
        {
            bool hasRange = game.TryRange(
                unit,
                out int range,
                out RangeShape shape);

            if (hasRange)
            {
                PaintRange(
                    center,
                    range,
                    shape,
                    rangeColor);
            }
        }

        foreach (Vector2Int cell in area.Cells)
        {
            bool hasTile = area.Board.TryGetCell(cell, out Tile tile);
            if (hasTile)
            {
                Paint(tile, placeColor);
            }
        }
    }

    private void PaintRange(
        Tile center,
        int range,
        RangeShape shape,
        Color color)
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
        List<Tile> tiles = TileShapeQuery.GetTiles(
            center.Board,
            center.Coord,
            range,
            shape);

        foreach (Tile tile in tiles)
        {
            Paint(tile, color);
        }
    }

    private void Paint(Tile tile, Color color)
    {
        painter.SetColor(tile, color);
        cellPainted.Add(tile);
    }
}
