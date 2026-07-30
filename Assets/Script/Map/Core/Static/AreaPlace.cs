using UnityEngine;

// 계산된 자리(PlacementArea)를 실제 판에 반영하는 담당 — 놓을 수 있는지 보고, 칸을 채우고, 다시 비운다.
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

    // 유닛이 설 높이 — 놓을 수 있는 칸들 중 가장 높은 윗면.
    public static float TopY(PlacementArea area, OccupantKind kind)
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

        return top > float.MinValue ? top : area.Center.y;   // 놓을 수 있는 칸이 하나도 없을 때만 앵커 높이
    }

    // 덮는 칸을 모두 점유하고 유닛을 한가운데 세운다. CanPlace가 통과한 자리에만 부른다.
    public static void Place(
        PlacementArea area,
        GameObject unit,
        OccupantKind kind,
        float yOffset)
    {
        float top = TopY(area, kind);

        foreach (Vector2Int cell in area.Cells)
        {
            if (!area.Board.TryGetCell(cell, out Tile tile))
            {
                continue;   // CanPlace를 건너뛰고 불렀을 때만 닿는다
            }

            tile.SetOccupant(unit, kind);
        }

        Vector3 position = area.Center;
        position.y = top + yOffset;
        unit.transform.position = position;
    }

    // 유닛이 덮고 있던 칸을 모두 비우고, 그 칸들이 이루는 크기를 돌려준다.
    // 점유 좌표를 따로 보관하지 않으므로 판을 훑어 되짚는다(제거는 드물다는 전제).
    public static Vector2Int Remove(
        Tile source,
        GameObject unit)
    {
        MapBoard board = source.Board;

        // 사거리 커버 해제가 여기 딸려 있어 먼저 부른다. 이 호출로 source 칸은 이미 비워진다.
        board.RemoveUnit(source.Coord);

        int minCol = source.Coord.x;
        int maxCol = source.Coord.x;
        int minRow = source.Coord.y;
        int maxRow = source.Coord.y;

        foreach (Tile tile in board.Cells.Values)
        {
            if (tile.OccupantObject != unit)
            {
                continue;
            }

            Vector2Int coord = tile.Coord;
            if (coord.x < minCol) { minCol = coord.x; }
            if (coord.x > maxCol) { maxCol = coord.x; }
            if (coord.y < minRow) { minRow = coord.y; }
            if (coord.y > maxRow) { maxRow = coord.y; }

            tile.ClearOccupant();
        }

        return new Vector2Int(maxCol - minCol + 1, maxRow - minRow + 1);
    }
}
