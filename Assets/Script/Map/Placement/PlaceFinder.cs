using UnityEngine;

// 포인터가 가리키는 자리를 찾아준다. 계산만 하고 판을 바꾸지 않는다.
public class PlaceFinder
{
    private readonly PointerPick pointerPick;
    private readonly PlacePalette palette;

    public PlaceFinder(
        PointerPick pointerPick,
        PlacePalette palette)
    {
        this.pointerPick = pointerPick;
        this.palette = palette;
    }

    // 종류와 크기로 자리를 찾는다. 가리키는 자리가 없으면 false.
    public bool TryResolve(
        OccupantKind kind,
        Vector2Int size,
        out PlaceData data)
    {
        PlacementArea area = pointerPick.GetArea(size);
        if (area == null)
        {
            data = default;
            return false;
        }

        data = new PlaceData(area, AreaPlace.TopY(area, kind), AreaPlace.CanPlace(area, kind));
        return true;
    }

    // 팔레트가 고른 슬롯으로 자리를 찾는다. 슬롯이 비었으면 false.
    public bool TryResolveSlot(
        out Placeable slot,
        out PlaceData data)
    {
        data = default;

        if (!palette.TryCurrentSlot(out slot))
        {
            return false;
        }

        return TryResolve(slot.kind, PlaceSize.GetSize(slot), out data);
    }
}
