using System.Collections.Generic;
using UnityEngine;

// 이번 프레임에 칠할 목록을 정한다. 지난 프레임과 같으면 아무것도 내주지 않는다.
public class TilePaintSync
{
    private readonly PlaceHoverFinder hoverFinder;
    private readonly SkillTargetFinder skillFinder;
    private readonly RangeCalc rangeCalc;
    private readonly RangeTileData rangeStore;
    private readonly TilePainter painter;

    private readonly List<PaintEntry> plan = new();
    private TileDisplayData lastDisplay;
    private bool hasDisplay;

    public TilePaintSync(
        PlaceHoverFinder hoverFinder,
        SkillTargetFinder skillFinder,
        RangeCalc rangeCalc,
        RangeTileData rangeStore,
        TilePainter painter)
    {
        this.hoverFinder = hoverFinder;
        this.skillFinder = skillFinder;
        this.rangeCalc = rangeCalc;
        this.rangeStore = rangeStore;
        this.painter = painter;
    }

    public bool TryBuildPlan(out List<PaintEntry> result)
    {
        result = plan;

        HoverMode mode = hoverFinder.FindHover(out PlaceData placeData, out GameObject unit);
        skillFinder.TryFindTarget(out Hero caster, out HeroActiveSkill skill, out Tile skillOrigin);
        int rangeVersion = ResolveRangeVersion(mode);

        TileDisplayData display = BuildDisplayKey(
            mode, placeData.Area, unit, placeData.CanPlace, rangeVersion,
            caster, skill, skillOrigin);

        if (IsSameDisplay(display))
        {
            return false;
        }

        RebuildPlan(mode, placeData, unit, skill, skillOrigin);

        lastDisplay = display;
        hasDisplay = true;
        return true;
    }

    // ---- 이번 프레임 표시 값 조립·비교 ----

    private int ResolveRangeVersion(HoverMode mode)
    {
        if (IsRangeHoverMode(mode))
        {
            return rangeStore.Version;
        }

        return 0;
    }

    private static TileDisplayData BuildDisplayKey(
        HoverMode mode,
        PlacementArea area,
        GameObject unit,
        bool canPlace,
        int rangeVersion,
        Hero skillCaster,
        HeroActiveSkill skill,
        Tile skillOrigin)
    {
        ResolveAreaOrigin(area, out MapBoard board, out Vector2Int origin);

        return new TileDisplayData(
            mode, board, origin, unit, canPlace, rangeVersion,
            skillCaster, skill, skillOrigin);
    }

    private static void ResolveAreaOrigin(PlacementArea area, out MapBoard board, out Vector2Int origin)
    {
        board = null;
        origin = default;

        if (HasArea(area))
        {
            board = area.Board;
            origin = area.Origin;
        }
    }

    private bool IsSameDisplay(TileDisplayData display)
    {
        if (HasDisplay())
        {
            return display.SameAs(lastDisplay);
        }

        return false;
    }

    private bool HasDisplay()
    {
        return hasDisplay;
    }

    // ---- 칠할 목록 조립 ----

    private void RebuildPlan(HoverMode mode, PlaceData data, GameObject unit, HeroActiveSkill skill, Tile skillOrigin)
    {
        plan.Clear();
        AddHoverEntries(mode, data, unit);
        AddSkillEntries(skill, skillOrigin);
    }

    private void AddHoverEntries(HoverMode mode, PlaceData data, GameObject unit)
    {
        if (IsPlacingHoverMode(mode))
        {
            AddAreaPreviewEntries(data, unit);
            return;
        }

        if (IsHeldHoverMode(mode))
        {
            AddAreaPreviewEntries(data, unit);
            return;
        }

        if (IsRangeHoverMode(mode))
        {
            AddUnitRangeEntries();
        }
    }

    private bool IsPlacingHoverMode(HoverMode mode)
    {
        return mode == HoverMode.Placing;
    }

    private bool IsHeldHoverMode(HoverMode mode)
    {
        return mode == HoverMode.Held;
    }

    private static bool IsRangeHoverMode(HoverMode mode)
    {
        return mode == HoverMode.Range;
    }

    private void AddAreaPreviewEntries(PlaceData data, GameObject unit)
    {
        if (HasArea(data.Area))
        {
            AddPreviewEntries(data, unit);
        }
    }

    private static bool HasArea(PlacementArea area)
    {
        return area != null;
    }

    private void AddPreviewEntries(PlaceData data, GameObject unit)
    {
        Color placeColor = ResolvePlaceColor(data);
        Color rangeColor = ResolveRangeColor(data);

        if (data.Area.Board.TryGetCell(data.Area.Origin, out Tile center))
        {
            if (rangeCalc.TryGetRangeAtCenter(unit, center, out List<Tile> range))
            {
                AddTileEntries(range, rangeColor);
            }
        }

        AddAreaCellEntries(data.Area, placeColor);
    }

    private bool CanPlaceHere(PlaceData data)
    {
        return data.CanPlace;
    }

    private Color ResolvePlaceColor(PlaceData data)
    {
        if (CanPlaceHere(data))
        {
            return painter.okColor;
        }

        return painter.denyColor;
    }

    private Color ResolveRangeColor(PlaceData data)
    {
        if (CanPlaceHere(data))
        {
            return painter.rangeColor;
        }

        return painter.denyColor;
    }

    private void AddAreaCellEntries(PlacementArea area, Color color)
    {
        for (int i = 0; i < area.Cells.Count; i++)
        {
            if (area.Board.TryGetCell(area.Cells[i], out Tile tile))
            {
                plan.Add(new PaintEntry(tile, color));
            }
        }
    }

    private void AddUnitRangeEntries()
    {
        for (int i = 0; i < rangeStore.Tiles.Count; i++)
        {
            plan.Add(new PaintEntry(rangeStore.Tiles[i], painter.rangeColor));
        }
    }

    private void AddSkillEntries(HeroActiveSkill skill, Tile origin)
    {
        if (HasTile(origin))
        {
            List<Tile> hitRange = SkillRangeCalc.BuildHitRange(skill, origin);
            AddTileEntries(hitRange, painter.skillColor);
        }
    }

    private static bool HasTile(Tile tile)
    {
        return tile != null;
    }

    private void AddTileEntries(List<Tile> tiles, Color color)
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            plan.Add(new PaintEntry(tiles[i], color));
        }
    }
}
