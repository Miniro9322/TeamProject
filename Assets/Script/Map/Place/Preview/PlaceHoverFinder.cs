using UnityEngine;

// 이번 프레임에 배치/재배치 호버가 가리키는 자리를 찾는다. 칠하지 않는다.
public class PlaceHoverFinder
{
    private readonly MapView game;
    private readonly PlaceFinder finder;
    private readonly HoverPlaceData hoverPlace;

    public PlaceHoverFinder(MapView game, PlaceFinder finder, HoverPlaceData hoverPlace)
    {
        this.game = game;
        this.finder = finder;
        this.hoverPlace = hoverPlace;
    }

    public HoverMode FindHover(out PlaceData placeData, out GameObject unit)
    {
        HoverMode mode = ResolveHoverMode();
        ResolveHover(mode, out placeData, out unit);
        UpdateHoverPlace(mode, placeData);
        return mode;
    }

    private HoverMode ResolveHoverMode()
    {
        if (IsInputBlocked())
        {
            return HoverMode.Blocked;
        }

        if (IsPlacingMode())
        {
            return HoverMode.Placing;
        }

        if (IsHeldReplaceMode())
        {
            return HoverMode.Held;
        }

        return HoverMode.Range;
    }

    private bool IsInputBlocked()
    {
        return game.InputBlocked;
    }

    private bool IsPlacingMode()
    {
        return game.IsPlacing;
    }

    private bool IsHeldReplaceMode()
    {
        if (IsReplacingMode())
        {
            return IsHoldingUnit();
        }

        return false;
    }

    private bool IsReplacingMode()
    {
        return game.IsReplacing;
    }

    private bool IsHoldingUnit()
    {
        return game.IsHolding;
    }

    private bool IsPlacingHoverMode(HoverMode mode)
    {
        return mode == HoverMode.Placing;
    }

    private bool IsHeldHoverMode(HoverMode mode)
    {
        return mode == HoverMode.Held;
    }

    private void ResolveHover(HoverMode mode, out PlaceData placeData, out GameObject unit)
    {
        placeData = default;
        unit = null;

        if (IsPlacingHoverMode(mode))
        {
            ResolvePlacingHover(out placeData, out unit);
            return;
        }

        if (IsHeldHoverMode(mode))
        {
            ResolveHeldHover(out placeData, out unit);
        }
    }

    private void ResolvePlacingHover(out PlaceData placeData, out GameObject unit)
    {
        placeData = default;
        unit = null;

        if (finder.TryResolveSlot(out Placeable slot, out placeData))
        {
            unit = slot.prefab;
        }
    }

    private void ResolveHeldHover(out PlaceData placeData, out GameObject unit)
    {
        unit = game.HeldUnit;
        finder.TryResolve(game.HeldKind, game.HeldSize, out placeData);
    }

    private void UpdateHoverPlace(HoverMode mode, PlaceData placeData)
    {
        if (IsPlacingHoverMode(mode))
        {
            KeepOrClearHover(placeData);
            return;
        }

        if (IsHeldHoverMode(mode))
        {
            KeepOrClearHover(placeData);
            return;
        }

        hoverPlace.Clear();
    }

    private void KeepOrClearHover(PlaceData placeData)
    {
        if (HasArea(placeData.Area))
        {
            hoverPlace.Keep(placeData);
            return;
        }

        hoverPlace.Clear();
    }

    private static bool HasArea(PlacementArea area)
    {
        return area != null;
    }
}
