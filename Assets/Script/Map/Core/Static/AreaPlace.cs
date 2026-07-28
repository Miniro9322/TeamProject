using UnityEngine;

public static class AreaPlace
{
    public static bool CanPlace(
        PlacementArea area,
        OccupantKind kind)
    {
        foreach (Vector2Int cell in area.Cells)
        {
            if (!area.Board.CanPlace(cell, kind))
            {
                return false;
            }
        }

        return true;
    }

    public static void Place(
        PlacementArea area,
        GameObject unit,
        OccupantKind kind,
        float yOffset)
    {
        float top = area.Center.y;

        foreach (Vector2Int cell in area.Cells)
        {
            area.Board.TryGetCell(cell, out Tile tile);

            if (tile.WorldTop.y > top)
            {
                top = tile.WorldTop.y;
            }

            tile.SetOccupant(unit, kind);
        }

        Vector3 position = area.Center;
        position.y = top + yOffset;
        unit.transform.position = position;
    }

    public static void Remove(
        Tile source,
        GameObject unit)
    {
        MapBoard board = source.Board;

        board.RemoveUnit(source.Coord);

        foreach (Tile tile in board.Cells.Values)
        {
            if (tile.OccupantObject != unit)
            {
                continue;
            }

            tile.ClearOccupant();
        }
    }
}
