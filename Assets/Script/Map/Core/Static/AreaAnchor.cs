using System.Collections.Generic;
using UnityEngine;

// 커서 광선과 점유 칸 크기로 배치물이 놓일 자리를 정하는 계산 전담 정적 클래스.
public static class AreaAnchor
{
    // 커서 아래 타일을 받아 시작 칸·덮는 칸·설 자리를 한 번에 낸다.
    public static PlacementArea Resolve(Tile anchor, Ray ray, Vector2Int size, float cellSize)
    {
        Vector2Int origin = Snap(anchor, ray, size, cellSize);
        List<Vector2Int> cells = AreaCalc.GetCells(origin, size);

        return new PlacementArea(anchor.Board, origin, size, cells, Center(anchor, origin, size, cellSize));
    }

    // 커서를 한가운데 두는 시작 칸을 구한다. 덮는 칸이 맵 밖으로 나가면 안쪽으로 되돌린다.
    public static Vector2Int Snap(Tile anchor, Ray ray, Vector2Int size, float cellSize)
    {
        Vector2 point = CellPoint(anchor, ray, cellSize);

        Vector2Int origin = new(
            Mathf.FloorToInt(point.x - size.x * 0.5f + 0.5f),
            Mathf.FloorToInt(point.y - size.y * 0.5f + 0.5f));

        return Clamp(origin, size, anchor.Board.PlayRect);
    }

    // 덮는 칸이 모두 맵 안쪽에 들어오도록 시작 칸을 옮긴다.
    // 커서가 장식 줄이나 맵 밖으로 나가도 가장자리 자리에서 멈추므로, 절반만 맵에 걸친 자리가 나오지 않는다.
    private static Vector2Int Clamp(Vector2Int origin, Vector2Int size, RectInt rect)
    {
        if (rect.width < size.x || rect.height < size.y)
        {
            return origin;   // 배치물이 맵보다 넓으면 옮겨도 들어갈 수 없다 — 배치 판정이 거부한다
        }

        return new Vector2Int(
            Mathf.Clamp(origin.x, rect.xMin, rect.xMax - size.x),
            Mathf.Clamp(origin.y, rect.yMin, rect.yMax - size.y));
    }

    // 덮는 칸들의 한가운데 월드 지점을 구한다(유닛 몸통이 설 자리).
    public static Vector3 Center(Tile anchor, Vector2Int origin, Vector2Int size, float cellSize)
    {
        Vector3 top = anchor.WorldTop;
        float dx = origin.x + (size.x - 1) * 0.5f - anchor.Coord.x;
        float dz = origin.y + (size.y - 1) * 0.5f - anchor.Coord.y;

        // 높이는 앵커 타일 기준. 칸마다 다른 경우의 정확한 값은 타일을 이미 훑는 배치 단계에서 정한다.
        return new Vector3(top.x + dx * cellSize, top.y, top.z + dz * cellSize);
    }

    // 광선이 앵커 타일 윗면과 만나는 지점을 연속 칸 좌표로 바꾼다.
    private static Vector2 CellPoint(Tile anchor, Ray ray, float cellSize)
    {
        Vector3 top = anchor.WorldTop;

        if (Mathf.Abs(ray.direction.y) < 1e-6f)
        {
            return new Vector2(anchor.Coord.x + 0.5f, anchor.Coord.y + 0.5f);   // 수평 시선 — 앵커 칸 한가운데로 본다
        }

        float t = (top.y - ray.origin.y) / ray.direction.y;
        Vector3 hit = ray.origin + ray.direction * t;

        return new Vector2(
            anchor.Coord.x + 0.5f + (hit.x - top.x) / cellSize,
            anchor.Coord.y + 0.5f + (hit.z - top.z) / cellSize);
    }
}
