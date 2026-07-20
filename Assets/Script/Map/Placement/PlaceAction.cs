using UnityEngine;

// 고른 타일에 선택·배치·제거·집기·내려놓기를 수행하고 결과를 MapView에 알린다.
public class PlaceAction
{
    // MapCommand가 조립할 때 넣어준다
    public PlacePalette palette;
    public UnitPlacer placer;
    public UnitRemover remover;
    public UnitReplace replace;
    public BuildingUiLink buildingUi;
    public MapView view;
    public float placeYOffset;

    public void SelectTile(Tile tile)
    {
        view.Select(tile);
        view.SetStatus($"{tile.Coord} 선택");
    }

    public void PlaceUnit(Tile tile)
    {
        if (!palette.TryCurrentSlot(out Placeable slot)) return;   // 슬롯 없으면 중단(검사 게이트)
        if (!tile.Board.CanPlace(tile.Coord, slot.kind, out _)) return; // 배치 불가면 중단(클릭된 타일이 속한 모듈 보드 기준)
        if (!buildingUi.CanBuild()) { Debug.Log("밤에는 배치할 수 없습니다."); return; } // 테스트용

        if (placer.TryPlace(tile, slot, placeYOffset, out _, out string reason))
        {
            view.Select(tile);
        }
        else
        {
            view.SetStatus($"{tile.Coord} {reason}");
        }
    }

    public void RemoveUnit(Tile tile)
    {
        if (!remover.TryRemoveUnit(tile)) return;
        if (view.IsSelected(tile)) view.ClearSelection();
        view.SetStatus($"{tile.Coord} 제거");
    }

    public void PickUpUnit(Tile tile)
    {
        if (!replace.PickUp(tile)) return;
        view.Select(tile);
        view.SetStatus(replace.HeldInfo);
    }

    public void Drop(Tile tile)
    {
        bool moved = replace.TryDrop(tile, placeYOffset, out string message);
        view.SetStatus(message);
        if (moved) view.Select(tile);
    }

    public void ClearAllPlacedUnit()
    {
        replace.CancelHeldAndDestroy();
        remover.RemoveAll();
        view.ClearSelection();
        view.SetStatus("배치 전부 제거");
    }
}
