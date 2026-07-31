using UnityEngine;

// 배치된 유닛을 집어 다른 자리에 다시 놓는(재배치, re-place) 담당. 집은 유닛(프리뷰)과 그 정보를 들고 있는다.
// 집을 땐 출발 타일의 보드에서 떼고, 놓을 땐 목표 자리의 보드에 놓는다 → 모듈 사이 이동도 자연히 성립.
public class UnitReplace
{
    private readonly UnitList _unitList;

    private GameObject _heldUnit;   // 집어 든 유닛(보드에서 뗀 채 포인터를 따라간다).
    private Tile _heldFromTile;     // 집은 출발 칸(제자리 판별용).
    private OccupantKind _heldKind;
    private int _heldRange;
    private Vector2Int _heldSize = Vector2Int.one;   // 집기 전에 덮고 있던 칸 수 — 내려놓을 때 같은 크기로 돌아간다.

    public UnitReplace(UnitList unitList)
    {
        _unitList = unitList;
    }

    public bool IsHolding => _heldUnit != null;
    public Tile HeldFromTile => _heldFromTile;
    public GameObject HeldUnit => _heldUnit;
    public OccupantKind HeldKind => _heldKind;
    public Vector2Int HeldSize => _heldSize;

    // 칸의 유닛을 집어 든다: 덮고 있던 칸을 모두 떼고 프리뷰 상태로 전환. 성공하면 true(빈 칸이면 false).
    public bool PickUp(Tile tile)
    {
        GameObject unit = tile.OccupantObject;
        if (unit == null)
        {
            return false;
        }

        OccupantKind kind = tile.State.Occupant;   // 떼기 전에 종류를 읽어 둔다(ClearOccupant가 None으로 바꿈).
        _heldSize = AreaPlace.Remove(tile, unit);  // 여러 칸을 덮고 있었으면 전부 비우고 그 크기를 받아 둔다
        _heldUnit = unit;
        _heldFromTile = tile;
        _heldKind = kind;
        _unitList.TryGetRange(unit, out int range);
        _heldRange = range;
        return true;
    }

    // 집은 유닛 프리뷰를 목표 자리 한가운데로 옮긴다(포인터 따라다니기).
    public void MoveHeldTo(PlaceData data, float yOffset)
    {
        Vector3 position = data.Area.Center;
        position.y = data.TopY + yOffset;
        _heldUnit.transform.position = position;
    }

    // 집은 유닛을 자리에 내려놓는다. 배치 규칙 통과 시 재배치하고 true, 아니면 집은 채 유지한다.
    // 목표 자리의 보드 기준이라, 다른 모듈에 놓으면 커버도 그 모듈에 등록된다(다른 모듈 공격 금지 요구 충족).
    public bool TryDrop(PlaceData data, float yOffset)
    {
        if (!data.CanPlace)
        {
            return false;
        }

        AreaPlace.Place(data, _heldUnit, _heldKind, yOffset);
        RegisterCover(data.Area);
        //
        Hero hero = _heldUnit.GetComponent<Hero>();
        if (hero != null)
        {
            hero.SetBoard(data.Area.Board);
            hero.SetCurrentTile();
        }
        //
        ClearHeld();
        return true;
    }

    // 집은 유닛을 파괴하고 프리뷰 상태를 해제(전체 제거 시 보드에 없는 프리뷰 고아 방지).
    public void CancelHeldAndDestroy()
    {
        Object.Destroy(_heldUnit);
        ClearHeld();
    }

    // 재배치한 유닛의 사거리 커버를 "내려놓은 자리의" 보드에 다시 등록(건물은 사거리 없음 → 제외).
    private void RegisterCover(PlacementArea area)
    {
        if (_heldKind == OccupantKind.Building) return;   // 건물은 사거리 없음(널 방어 아님·실제 분기).
        area.Board.SetRangeCover(_heldUnit, area.Origin, Mathf.Max(0, _heldRange));
    }

    private void ClearHeld()
    {
        _heldUnit = null;
        _heldFromTile = null;
        _heldKind = OccupantKind.None;
        _heldRange = 0;
        _heldSize = Vector2Int.one;
    }
}
