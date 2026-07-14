using UnityEngine;

/// <summary>
/// 점령 영역이 L자로 확장되는 "바깥 전진 모서리"만 Gizmo로 옅게 표시한다(확장 확인용).
/// AreaExpand(내부 기능)는 건드리지 않고 보드의 Territory 상태만 읽는다. 렌더러/머티리얼 미변경.
/// </summary>
[ExecuteAlways]
public class AreaOutline : MonoBehaviour
{
    [SerializeField] private MapBoard _board;                          // 인스펙터 주입(자동탐색은 폴백만).
    [SerializeField] private Color _tint = new(0.35f, 0.8f, 1f, 0.22f); // 옅게: 낮은 알파로 티만 나게.
    [SerializeField] private float _yOffset = 0.02f;                   // 타일 윗면에서 띄우는 높이.
    [SerializeField] private float _size = 0.92f;                      // 칸보다 살짝 작게.
    [SerializeField] private float _height = 0.35f;                    // 세로 두께: 아이소 각도에서 옆에서도 보이게.

    private void OnValidate()
    {
        if (_board == null)
        {
            _board = GetComponent<MapBoard>();
        }
    }

    private void OnDrawGizmos()
    {
        if (_board == null || _board.CellCount == 0)
        {
            return;
        }

        // 테두리 선은 채움보다 또렷하게(옆에서도 링이 읽히도록).
        Color edge = new(_tint.r, _tint.g, _tint.b, Mathf.Min(1f, _tint.a * 3f));

        foreach (Tile tile in _board.Cells.Values)
        {
            if (!IsFrontier(tile))
            {
                continue;
            }

            // 타일 윗면에 서 있는 얇은 세로 박스 → 낮은 아이소 각도에서도 옆면이 보인다.
            Vector3 pos = tile.WorldTop + Vector3.up * (_yOffset + _height * 0.5f);
            Vector3 box = new(_size, _height, _size);

            Gizmos.color = _tint;
            Gizmos.DrawCube(pos, box);
            Gizmos.color = edge;
            Gizmos.DrawWireCube(pos, box);
        }
    }

    // 바깥 L 모서리 = 점령 타일이면서, 확장 방향(+x/+y)의 이웃이 점령이 아닌 칸.
    // "점령 아님"에는 미점령·테두리·맵 밖이 모두 포함 → 완전 확장해 맵 경계에 닿아도 바깥 모서리가 유지된다.
    // AreaExpand가 본진 코너에서 +x/+y로만 확장하므로 두 방향만 검사 → 안쪽(-x/-y) 모서리는 안 칠함.
    private bool IsFrontier(Tile tile)
    {
        if (tile == null || tile.State.Territory != TerritoryState.Claimed)
        {
            return false;
        }

        Vector2Int coord = tile.Coord;
        return !IsClaimed(coord + GridCalculator.Right) // +x 이웃이 점령 아님 → 바깥 모서리
            || !IsClaimed(coord + GridCalculator.Up);   // +y 이웃이 점령 아님 → 바깥 모서리
    }

    // 해당 좌표에 점령된 타일이 있는가. 칸이 없으면(맵 밖) false.
    private bool IsClaimed(Vector2Int coord)
    {
        return _board.TryGetCell(coord, out Tile tile)
            && tile.State.Territory == TerritoryState.Claimed;
    }
}
