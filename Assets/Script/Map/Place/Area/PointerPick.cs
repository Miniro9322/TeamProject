using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 마우스 포인터 아래의 타일을 모든 모듈 보드에서 찾는다(그리드 밖이면 가장 가까운 가장자리 타일).
// 여러 보드가 동시에 맞으면 카메라에 가장 가까운 타일을 고른다 → 그 타일의 Board가 곧 클릭된 모듈.
// 배치물이 여러 칸을 차지할 때는 타일 하나로 부족하므로 놓일 자리(PlacementArea)까지 내준다.
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

    // 포인터 아래에 배치물이 몇 칸으로 어디에 놓일지. 그리드 밖이면 가장자리 타일을 기준으로 잡는다.
    public PlacementArea GetArea(Vector2Int size)
    {
        Ray ray = PointerRay();
        Tile anchor = Nearest(ray);
        if (anchor == null)
        {
            return null;
        }

        // 앵커와 스냅이 같은 광선에서 나와야 한다 — 따로 뽑으면 포인터가 움직인 만큼 어긋난다.
        return AreaAnchor.Resolve(anchor, ray, size);
    }

    public Tile UnderPointer()   // 없으면 null
    {
        return Under(PointerRay());
    }

    public Tile NearestCell()
    {
        return Nearest(PointerRay());
    }

    private Tile Under(Ray ray)
    {
        // 격자 평면 수학만으로는 캐릭터 모델 위(발밑 타일 바깥으로 튀어나온 부분)를 클릭해도
        // 못 잡는다 — 영웅 콜라이더를 먼저 물리 레이캐스트로 검사해 몸통 직접 클릭을 우선 처리.
        if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.GetComponentInParent<Hero>() is Hero hero && hero.CurrentTile != null)
        {
            return hero.CurrentTile;
        }

        Tile best = null;
        float bestSqr = float.MaxValue;
        foreach (MapBoard board in _boards)
        {
            if (!board.gameObject.activeInHierarchy) continue;
            if (!board.IsUnlocked) continue;

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

    private Tile Nearest(Ray ray)
    {
        Tile best = null;
        float bestDist = float.MaxValue;
        foreach (MapBoard board in _boards)
        {
            if (!board.gameObject.activeInHierarchy) continue;
            if (!board.IsUnlocked) continue;

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
