using System.Collections.Generic;
using UnityEngine;

// 모닥불이 보호하는 실제 보드 좌표를 보관합니다.
public class CampfireData
{
    private readonly HashSet<Vector2Int> cells = new();

    public IReadOnlyCollection<Vector2Int> Cells => cells;
    public int Count => cells.Count;

    // 보호 좌표를 중복 없이 보관합니다.
    public void Keep(Vector2Int cell)
    {
        cells.Add(cell);
    }

    // 지정 좌표가 보호 영역인지 확인합니다.
    public bool Contains(Vector2Int cell)
    {
        return cells.Contains(cell);
    }
}
