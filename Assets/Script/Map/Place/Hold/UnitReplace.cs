using System;
using UnityEngine;

// 배치된 유닛을 집어 다른 자리에 다시 놓는 담당.
public class UnitReplace
{
    private readonly PlacedUnitData _unitList;

    // 지금 집어 든 것 하나.
    private HeldData held;

    public UnitReplace(PlacedUnitData unitList)
    {
        _unitList = unitList;
    }

    // 집었다/내려놨다가 바뀔 때마다 알린다(UI가 매 프레임 폴링하지 않게).
    public event Action OnHoldChanged;

    public bool IsHolding => held.Unit != null;
    public Tile HeldFromTile => held.FromTile;
    public GameObject HeldUnit => held.Unit;
    public OccupantKind HeldKind => held.Kind;
    public Vector2Int HeldSize => held.Size;

    // 칸의 유닛을 집어 든다. 빈 칸이면 false.
    public bool PickUp(Tile tile)
    {
        GameObject unit = tile.OccupantObject;
        if (unit == null)
        {
            return false;
        }

        if (!_unitList.TryGetArea(unit, out PlacementArea area))
        {
            return false;
        }

        OccupantKind kind = tile.State.Occupant;   // 칸을 비우면 None이 되므로 먼저 읽는다.
        Vector3 fromPosition = unit.transform.position;   // 비우기 전에 미리 읽는다(취소 시 되돌릴 자리).

        AreaPlace.Remove(area);
        _unitList.Remove(unit);

        held = new HeldData(unit, tile, area, fromPosition, kind, area.Size);
        OnHoldChanged?.Invoke();
        return true;
    }

    // 재배치를 취소하고 집었던 자리에 그대로 되돌린다.
    public void ReturnHeld()
    {
        PlaceData data = new PlaceData(held.FromArea, held.FromPosition, true);
        AreaPlace.Place(data, held.Unit, held.Kind);
        _unitList.Add(held.Unit, held.FromArea);

        ClearHeld();
    }

    // 집은 유닛을 목표 자리 한가운데로 옮긴다.
    public void MoveHeldTo(PlaceData data)
    {
        held.Unit.transform.position = data.Position;
    }

    // 집은 유닛을 자리에 내려놓는다. 못 놓으면 false.
    public bool TryDrop(PlaceData data)
    {
        if (!data.CanPlace)
        {
            return false;
        }

        AreaPlace.Place(data, held.Unit, held.Kind);
        _unitList.Add(held.Unit, data.Area);
        //
        Hero hero = held.Unit.GetComponent<Hero>();
        if (hero != null)
        {
            hero.SetBoard(data.Area.Board);
            hero.SetCurrentTile();
        }
        //
        ClearHeld();
        return true;
    }

    // 집은 유닛을 파괴하고 집은 상태를 해제한다.
    public void CancelHeldAndDestroy()
    {
        UnityEngine.Object.Destroy(held.Unit);
        ClearHeld();
    }

    private void ClearHeld()
    {
        held = default;
        OnHoldChanged?.Invoke();
    }
}
