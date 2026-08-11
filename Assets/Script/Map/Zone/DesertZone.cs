using UnityEngine;

// 바람 방향은 하루가 바뀔 때마다 ZoneEffectApplier가 재배정한다(고정값 아님).
public class DesertZone : MonoBehaviour
{
    [SerializeField] private ZoneStatEffect[] effects;

    [SerializeField] private Vector2Int windDirection = GridCalculator.Right;

    public ZoneStatEffect[] Effects => effects;
    public Vector2Int WindDirection => windDirection;

    // 바람 방향을 새 값으로 바꿔 저장한다.
    public void SetWindDirection(Vector2Int direction)
    {
        windDirection = direction;
    }
}
