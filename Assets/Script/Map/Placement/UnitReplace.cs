using UnityEngine;

// 배치된 유닛을 집어 다른 칸에 다시 놓는(재배치, re-place) 담당. 집은 유닛(프리뷰)과 그 정보를 들고 있는다.
// 집을 땐 출발 타일의 보드에서 떼고, 놓을 땐 목표 타일의 보드에 놓는다 → 모듈 사이 이동도 자연히 성립.
public class UnitReplace
{
    private readonly UnitList _unitList;

    private GameObject _heldUnit;   // 집어 든 유닛(보드에서 뗀 채 포인터를 따라간다).
    private Tile _heldFromTile;     // 집은 출발 칸(제자리 판별·정보 표시용).
    private OccupantKind _heldKind;
    private int _heldRange;

    public UnitReplace(UnitList unitList)
    {
        _unitList = unitList;
    }

    public bool IsHolding => _heldUnit != null;
    public Tile HeldFromTile => _heldFromTile;

    // 패널 Status에 그대로 표시되는 "집은 유닛 정보" 문자열.
    public string HeldInfo
    {
        get
        {
            if (_heldUnit == null) return "";
            return $"{_heldKind} 이동 중 (출발 {_heldFromTile.Coord}, 사거리 {_heldRange})";
        }
    }

    // 칸의 유닛을 집어 든다: 보드에서 떼고 프리뷰 상태로 전환. 성공하면 true(빈 칸이면 false).
    public bool PickUp(Tile tile)
    {
        OccupantKind kind = tile.State.Occupant;   // 떼기 전에 종류를 읽어 둔다(ClearOccupant가 None으로 바꿈).
        GameObject unit = tile.Board.RemoveUnit(tile.Coord);   // 출발 타일이 속한 모듈 보드에서 뗀다
        _heldUnit = unit;
        _heldFromTile = tile;
        _heldKind = kind;
        _unitList.TryGetRange(unit, out int range);
        _heldRange = range;
        return true;
    }

    // 집은 유닛 프리뷰를 목표 칸 윗면으로 옮긴다(포인터 따라다니기).
    public void MoveHeldTo(Tile tile, float yOffset)
    {
        _heldUnit.transform.position = tile.WorldTop + Vector3.up * yOffset;
    }

    // 집은 유닛을 칸에 내려놓는다. 배치 규칙 통과 시 재배치하고 true, 아니면 사유(message) 반환하고 집은 채 유지.
    // 목표 타일의 보드 기준이라, 다른 모듈에 놓으면 커버도 그 모듈에 등록된다(다른 모듈 공격 금지 요구 충족).
    public bool TryDrop(Tile tile, float yOffset, out string message)
    {
        if (!tile.Board.CanPlace(tile.Coord, _heldKind, out string reason))
        {
            message = $"{tile.Coord} {reason}";
            return false;
        }

        tile.Board.TryPlace(tile.Coord, _heldUnit, _heldKind, yOffset, out _);
        RegisterCover(tile);
        message = $"{tile.Coord} 이동";
        ClearHeld();
        return true;
    }

    // 집은 유닛을 파괴하고 프리뷰 상태를 해제(전체 제거 시 보드에 없는 프리뷰 고아 방지).
    public void CancelHeldAndDestroy()
    {
        Object.Destroy(_heldUnit);
        ClearHeld();
    }

    // 재배치한 유닛의 사거리 커버를 "내려놓은 타일의" 보드에 다시 등록(건물은 사거리 없음 → 제외).
    private void RegisterCover(Tile tile)
    {
        if (_heldKind == OccupantKind.Building) return;   // 건물은 사거리 없음(널 방어 아님·실제 분기).
        tile.Board.SetRangeCover(_heldUnit, tile.Coord, Mathf.Max(0, _heldRange));
    }

    private void ClearHeld()
    {
        _heldUnit = null;
        _heldFromTile = null;
        _heldKind = OccupantKind.None;
        _heldRange = 0;
    }
}
