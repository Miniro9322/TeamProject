using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 마우스 포인터 아래의 타일을 모든 모듈 보드에서 찾는다(그리드 밖이면 가장 가까운 가장자리 타일).
// 여러 보드가 동시에 맞으면 카메라에 가장 가까운 타일을 고른다 → 그 타일의 Board가 곧 클릭된 모듈.
// 배치물이 여러 칸을 차지할 때는 타일 하나로 부족하므로 놓일 자리(PlacementArea)까지 내준다.
public class PointerPick
{
    private readonly List<MapBoard> boards;
    private readonly HoveredTileData hoverData;
    private int lastUpdateFrame = -1;

    public PointerPick(List<MapBoard> boards, HoveredTileData hoverData)
    {
        this.boards = boards;
        this.hoverData = hoverData;
    }

    private Ray PointerRay()
    {
        return Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
    }

    // 포인터 아래에 배치물이 몇 칸으로 어디에 놓일지. 그리드 밖이면 가장자리 타일을 기준으로 잡는다.
    public PlacementArea GetArea(Vector2Int size)
    {
        // 기준 칸과 자리 계산이 같은 광선에서 나와야 한다 — 따로 뽑으면 포인터가 움직인 만큼 어긋난다.
        Ray ray = PointerRay();
        Tile tile = Nearest(ray);
        return AreaAnchor.Resolve(tile, ray, size);
    }

    public Tile UnderPointer()   // 없으면 null
    {
        EnsureFrameHoverTile();
        return hoverData.HoveredTile;
    }

    public Tile NearestCell()
    {
        return Nearest(PointerRay());
    }

    private Vector2 lastMousePos;
    private bool mousePosInitialized;

    private void EnsureFrameHoverTile()
    {
        int currentFrame = Time.frameCount;
        if (lastUpdateFrame == currentFrame)
        {
            return;
        }

        lastUpdateFrame = currentFrame;
        Vector2 currentMousePos = Vector2.zero;
        if (Mouse.current != null)
        {
            currentMousePos = Mouse.current.position.ReadValue();
        }
        if (mousePosInitialized && currentMousePos == lastMousePos && hoverData.HoveredTile != null)
        {
            return;
        }

        mousePosInitialized = true;
        lastMousePos = currentMousePos;
        Tile tile = Under(PointerRay());
        hoverData.Keep(tile);
    }

    private Tile Under(Ray ray)
    {
        // 클릭은 언제나 타일이 받는다. 유닛 몸통을 먼저 잡으면, 몸통이 화면에서 덮는
        // 위쪽 칸을 눌렀을 때도 그 유닛의 발밑 칸이 선택된다.
        Tile best = null;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < boards.Count; i++)
        {
            MapBoard board = boards[i];
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
        for (int i = 0; i < boards.Count; i++)
        {
            MapBoard board = boards[i];
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
