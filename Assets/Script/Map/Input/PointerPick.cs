using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 마우스 포인터 아래의 타일을 모든 모듈 보드에서 찾는다(그리드 밖이면 가장 가까운 가장자리 타일).
// 여러 보드가 동시에 맞으면 카메라에 가장 가까운 타일을 고른다 → 그 타일의 Board가 곧 클릭된 모듈.
public class PointerPick
{
    private readonly List<MapBoard> _boards;

    public PointerPick(List<MapBoard> boards)
    {
        _boards = boards;
    }

    private Ray PointerRay()
    {
        return Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
    }

    public Tile UnderPointer()   // 없으면 null
    {
        Ray ray = PointerRay();
        Tile best = null;
        float bestSqr = float.MaxValue;
        foreach (MapBoard board in _boards)
        {
            if (board == null || !board.gameObject.activeInHierarchy) continue; // 비활성(잠금 등) 모듈은 후보에서 제외
            Tile tile = board.CellFromRay(ray);
            if (tile == null) continue; // 이 보드는 레이가 안 맞음 — 다음 모듈

            // 레이가 두 모듈에 다 걸치면 카메라(레이 원점)에 가까운 쪽이 앞에 보이는 타일이다.
            float sqr = (tile.WorldTop - ray.origin).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = tile;
            }
        }
        return best;
    }

    public Tile NearestCell()
    {
        Ray ray = PointerRay();
        Tile best = null;
        float bestDist = float.MaxValue;
        foreach (MapBoard board in _boards)
        {
            if (board == null || !board.gameObject.activeInHierarchy) continue; // 비활성(잠금 등) 모듈은 후보에서 제외
            Tile tile = board.NearestCellFromRay(ray);
            if (tile == null) continue;

            // 그리드 밖 클램프 후보끼리는 "레이 직선에서 얼마나 벗어났나"로 비교한다(포인터에 가장 붙은 타일).
            float dist = Vector3.Cross(ray.direction, tile.WorldTop - ray.origin).magnitude;
            if (dist < bestDist)
            {
                bestDist = dist;
                best = tile;
            }
        }
        return best;
    }
}
