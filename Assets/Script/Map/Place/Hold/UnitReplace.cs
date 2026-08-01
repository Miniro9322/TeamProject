using UnityEngine;

// 배치된 유닛을 집어 다른 자리에 다시 놓는(재배치, re-place) 담당. 집은 유닛(프리뷰)과 그 정보를 들고 있는다.
// 집을 땐 출발 타일의 보드에서 떼고, 놓을 땐 목표 자리의 보드에 놓는다 → 모듈 사이 이동도 자연히 성립.
public class UnitReplace
{
    private readonly PlacedUnitData _unitList;

    // 집어 든 것 하나. 다섯 값을 함께 채우고 함께 비운다.
    private HeldData held;

    public UnitReplace(PlacedUnitData unitList)
    {
        _unitList = unitList;
    }

    public bool IsHolding => held.Unit != null;
    public Tile HeldFromTile => held.FromTile;
    public GameObject HeldUnit => held.Unit;
    public OccupantKind HeldKind => held.Kind;
    public Vector2Int HeldSize => held.Size;

    // 칸의 유닛을 집어 든다: 덮고 있던 칸을 모두 떼고 프리뷰 상태로 전환. 성공하면 true(빈 칸이면 false).
    public bool PickUp(Tile tile)
    {
        GameObject unit = tile.OccupantObject;
        if (unit == null)
        {
            return false;
        }

        OccupantKind kind = tile.State.Occupant;         // 떼기 전에 종류를 읽어 둔다(ClearOccupant가 None으로 바꿈).
        Vector2Int size = AreaPlace.Remove(tile, unit);  // 여러 칸을 덮고 있었으면 전부 비우고 그 크기를 받아 둔다
        _unitList.TryGetRange(unit, out int range);

        held = new HeldData(unit, tile, kind, range, size);
        return true;
    }

    // 집은 유닛 프리뷰를 목표 자리 한가운데로 옮긴다(포인터 따라다니기).
    public void MoveHeldTo(PlaceData data)
    {
        held.Unit.transform.position = data.Position;
    }

    // 집은 유닛을 자리에 내려놓는다. 배치 규칙 통과 시 재배치하고 true, 아니면 집은 채 유지한다.
    // 목표 자리의 보드 기준이라, 다른 모듈에 놓으면 커버도 그 모듈에 등록된다(다른 모듈 공격 금지 요구 충족).
    public bool TryDrop(PlaceData data)
    {
        if (!data.CanPlace)
        {
            return false;
        }

        AreaPlace.Place(data, held.Unit, held.Kind);
        RegisterCover(data.Area);
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

    // 집은 유닛을 파괴하고 프리뷰 상태를 해제(전체 제거 시 보드에 없는 프리뷰 고아 방지).
    public void CancelHeldAndDestroy()
    {
        Object.Destroy(held.Unit);
        ClearHeld();
    }

    // 재배치한 유닛의 사거리 커버를 "내려놓은 자리의" 보드에 다시 등록(건물은 사거리 없음 → 제외).
    private void RegisterCover(PlacementArea area)
    {
        if (held.Kind == OccupantKind.Building) return;   // 건물은 사거리 없음(널 방어 아님·실제 분기).
        area.Board.SetRangeCover(held.Unit, area.Origin, Mathf.Max(0, held.Range));
    }

    private void ClearHeld()
    {
        held = default;
    }
}
