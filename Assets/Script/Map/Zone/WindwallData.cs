using System.Collections.Generic;
using UnityEngine;

// 가림막이 막아주는 칸을 바람 방향별로 나눠 보관합니다.
public class WindwallData
{
    private readonly Dictionary<Vector2Int, HashSet<Vector2Int>> armsByWind = new();

    // 네 방향 빈 세트를 미리 만들어 둡니다. 채울 때 방향 존재 검사가 필요 없어집니다.
    public WindwallData()
    {
        for (int index = 0; index < GridCalculator.Directions.Length; index++)
        {
            armsByWind[GridCalculator.Directions[index]] = new HashSet<Vector2Int>();
        }
    }

    // 이 바람 방향에서 보호되는 칸 하나를 보관합니다.
    public void KeepArm(Vector2Int wind, Vector2Int cell)
    {
        armsByWind[wind].Add(cell);
    }

    // 이 칸이 오늘 바람 방향의 가림막 팔에 들어 있는지 확인합니다.
    public bool HasArm(Vector2Int wind, Vector2Int cell)
    {
        return armsByWind[wind].Contains(cell);
    }
}
