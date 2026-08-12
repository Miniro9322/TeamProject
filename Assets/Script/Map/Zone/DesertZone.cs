using UnityEngine;

// 바람 방향은 하루가 바뀔 때마다 ZoneEffectApplier가 재배정한다(고정값 아님).
public class DesertZone : MonoBehaviour
{
    [SerializeField] private DebuffSO[] debuffs;

    [SerializeField] private Vector2Int windDirection = GridCalculator.Right;

    [SerializeField, Min(0.1f)] private float arrowSize = 0.75f;
    [SerializeField, Min(0f)] private float arrowHeight = 0.08f;
    [SerializeField] private Color arrowColor = Color.yellow;

    public DebuffSO[] Debuffs => debuffs;
    public Vector2Int WindDirection => windDirection;
    public float ArrowSize => arrowSize;
    public float ArrowHeight => arrowHeight;
    public Color ArrowColor => arrowColor;

    // 바람 방향을 새 값으로 바꿔 저장한다.
    public void SetWindDirection(Vector2Int direction)
    {
        windDirection = direction;
    }
}
