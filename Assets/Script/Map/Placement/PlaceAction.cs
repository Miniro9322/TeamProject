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
    public HeroRoster heroRoster;
    public float placeYOffset;

    public void SelectTile(Tile tile)
    {
        view.Select(tile);
    }

    public void PlaceUnit(Tile tile)
    {
        if (!palette.TryCurrentSlot(out Placeable slot)) return;   // 슬롯 없으면 중단(검사 게이트)
        HeroRosterEntry entry = palette.CurrentRuntimeEntry;       // 배치 전에 미리 캡처(성공 후 모드가 바뀔 수 있음)
        if (entry != null && entry.State == HeroRosterState.Placed) return; // 이미 배치된 엔트리(개체 하나뿐) 중복 배치 방지
        if (!tile.Board.CanPlace(tile.Coord, slot.kind)) return; // 배치 불가면 중단(클릭된 타일이 속한 모듈 보드 기준)
        if (!buildingUi.CanBuild()) { Debug.Log("밤에는 배치할 수 없습니다."); return; } // 테스트용

        if (placer.TryPlace(tile, slot, placeYOffset, out GameObject placedUnit))
        {
            if (entry != null)
            {
                entry.MarkPlaced(placedUnit);
                placedUnit.AddComponent<HeroRosterLink>().Entry = entry;
                heroRoster.NotifyStateChanged();
            }
            view.Select(tile);
            palette.ClearMode();   // 개체 하나뿐이니 배치 즉시 Place 모드 종료(연속 배치 방지)
        }
    }

    public void RemoveUnit(Tile tile)
    {
        if (!remover.TryRemoveUnit(tile)) return;
        if (view.IsSelected(tile)) view.ClearSelection();
    }

    public void PickUpUnit(Tile tile)
    {
        if (!replace.PickUp(tile)) return;
        view.Select(tile);
    }

    public void Drop(Tile tile)
    {
        if (replace.TryDrop(tile, placeYOffset)) view.Select(tile);
    }

    public void ClearAllPlacedUnit()
    {
        replace.CancelHeldAndDestroy();
        remover.RemoveAll();
        view.ClearSelection();
    }
}
