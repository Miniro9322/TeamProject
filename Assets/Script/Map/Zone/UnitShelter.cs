using System.Collections.Generic;
using UnityEngine;

// 밤에 읽은 유닛 좌표로 방향별 유닛 가림막을 조회합니다.
public class UnitShelter
{
    private readonly HashSet<Vector2Int> unitCells = new();

    // 동적으로 배치된 유닛 좌표를 한 번 읽어 점유 칸을 저장합니다.
    public UnitShelter(IReadOnlyList<Vector2Int> units)
    {
        Collect(units);
    }

    // 대상 바로 한 칸 뒤(바람이 불어온 쪽)에 다른 유닛이 있는지 조회합니다.
    public bool IsSheltered(Vector2Int target, Vector2Int wind)
    {
        return unitCells.Contains(target - wind);
    }

    // 이 칸에 유닛이 직접 서 있는지 조회합니다.
    public bool HasUnit(Vector2Int cell)
    {
        return unitCells.Contains(cell);
    }

    // 유닛 수가 매번 달라지는 좌표 목록을 한 번 훑어 점유 칸을 모읍니다.
    private void Collect(IReadOnlyList<Vector2Int> units)
    {
        for (int index = 0; index < units.Count; index++)
        {
            unitCells.Add(units[index]);
        }
    }
}
