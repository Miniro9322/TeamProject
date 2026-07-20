using UnityEngine;
using UnityEngine.InputSystem;

// 마우스 포인터 아래의 타일을 찾는다(그리드 밖이면 가장 가까운 가장자리 타일).
public class PointerPick
{
    private readonly MapBoard _board;

    public PointerPick(MapBoard board)
    {
        _board = board;
    }

    private Ray PointerRay()
    {
        return Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
    }

    public Tile UnderPointer()   // 없으면 null
    {
        return _board.CellFromRay(PointerRay());
    }

    public Tile NearestCell()
    {
        return _board.NearestCellFromRay(PointerRay());
    }
}
