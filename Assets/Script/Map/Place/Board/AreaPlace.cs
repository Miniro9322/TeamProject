using UnityEngine;

// 계산된 자리를 실제 판에 반영하는 담당.
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

    // 유닛 몸통이 설 월드 지점.
    public static Vector3 Position(
        PlacementArea area,
        OccupantKind kind,
        float yOffset)
    {
        Vector3 position = area.Center;
        position.y = TopY(area, kind) + yOffset;
        return position;
    }

    // 놓을 수 있는 칸들 중 가장 높은 윗면.
    private static float TopY(PlacementArea area, OccupantKind kind)
    {
        float top = float.MinValue;

        foreach (Vector2Int cell in area.Cells)
        {
            if (!area.Board.CanPlace(cell, kind))
            {
                continue;
            }

            if (area.Board.TryGetCell(cell, out Tile tile) && tile.WorldTop.y > top)
            {
                top = tile.WorldTop.y;
            }
        }

        return top > float.MinValue ? top : area.Center.y;
    }

    // 자리가 덮는 칸을 모두 채우고 유닛을 한가운데 세운다.
    public static void Place(
        PlaceData data,
        GameObject unit,
        OccupantKind kind)
    {
        foreach (Vector2Int cell in data.Area.Cells)
        {
            Tile tile = data.Area.Board.Cells[cell];
            tile.SetOccupant(unit, kind);
            FireReceiver.ReceiveEntry(tile, unit.transform);
        }

        unit.transform.position = data.Position;
    }

    // 자리가 덮는 칸을 모두 비운다.
    public static void Remove(PlacementArea area)
    {
        for (int i = 0; i < area.Cells.Count; i++)
        {
            Tile tile = area.Board.Cells[area.Cells[i]];
            GameObject unit = tile.ClearOccupant();
            FireReceiver.ReceiveExit(tile, unit.transform);
        }
    }
}
