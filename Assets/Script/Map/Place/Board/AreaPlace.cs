using UnityEngine;

// 계산된 자리를 실제 판에 반영하는 담당.
public static class AreaPlace
{
    // 유닛 몸통이 설 월드 지점. 이 자리에 놓을 수 있는지도 같은 순회에서 함께 알아낸다.
    public static Vector3 Position(
        PlacementArea area,
        OccupantKind kind,
        float yOffset,
        out bool canPlace)
    {
        Vector3 position = area.Center;
        position.y = TopY(area, kind, out canPlace) + yOffset;
        return position;
    }

    // 놓을 수 있는 칸들 중 가장 높은 윗면. 칸 하나라도 놓을 수 없으면 canPlace가 false로 나온다.
    private static float TopY(PlacementArea area, OccupantKind kind, out bool canPlace)
    {
        float top = float.MinValue;
        canPlace = true;

        foreach (Vector2Int cell in area.Cells)
        {
            if (!area.Board.CanPlace(cell, kind))
            {
                canPlace = false;
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
