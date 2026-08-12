using System.Collections.Generic;
using UnityEngine;

// 계산을 다시 하지 않고 저장된 모닥불 보호 결과만 조회합니다.
public static class CampfireQuery
{
    // 지정 좌표가 추위 보호 영역인지 확인합니다.
    public static bool IsProtected(CampfireData data, Vector2Int cell)
    {
        if (data == null)
        {
            return false;
        }

        return data.Contains(cell);
    }

    // 점유 좌표 중 하나라도 모닥불 보호 영역인지 확인합니다.
    public static bool HasProtected(
        CampfireData data,
        IReadOnlyList<Vector2Int> cells)
    {
        if (data == null)
        {
            return false;
        }

        for (int index = 0; index < cells.Count; index++)
        {
            if (data.Contains(cells[index]))
            {
                return true;
            }
        }

        return false;
    }
}
