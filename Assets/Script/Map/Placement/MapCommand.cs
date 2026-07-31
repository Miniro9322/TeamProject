using System;
using System.Collections.Generic;
using UnityEngine;

// 마우스 눌림/뗌을 받아 현재 모드에 맞는 동작을 고른다. 스스로 일하지 않고 PlaceAction에 시킨다.
public class MapCommand : MonoBehaviour
{
    [SerializeField] private MapInput input;
    [SerializeField] private PlacePalette palette;

    // MapAssemble이 조립할 때 넣어준다
    public PointerPick pointerPick;
    public DragDetect dragDetect;
    public DragDetect rightDragDetect;
    public UnitReplace replace;
    public BuildingUiLink buildingUi;
    public PlaceAction action;
    public PlaceGhost ghost;
    public PlaceFinder finder;
    public RangeCalc rangeCalc;
    public RangeStore rangeStore;
    public Dictionary<PlaceMode, Action<Tile>> dispatch;

    private void OnEnable()
    {
        input.Pressed += OnPress;
        input.Released += OnRelease;
        input.RightPressed += OnRightPress;
        input.RightReleased += OnRightRelease;
    }

    private void OnDisable()
    {
        input.Pressed -= OnPress;
        input.Released -= OnRelease;
        input.RightPressed -= OnRightPress;
        input.RightReleased -= OnRightRelease;
    }

    private void Update()
    {
        FollowHeld();
        FollowGhost();
        DropRange();
    }

    // 버튼에서 손을 떼면 사거리 표시를 끝낸다.
    // UI 위에서 떼면 뗌 신호가 오지 않으므로 눌림 상태를 직접 본다.
    private void DropRange()
    {
        if (input.LeftHolding)
        {
            return;
        }

        rangeStore.ClearRange();
    }

    // 배치 모드면 미리보기를 커서 자리에 세우고, 아니면 감춘다.
    private void FollowGhost()
    {
        if (!IsPlacing())
        {
            ghost.HideGhost();
            return;
        }

        // 프리팹 없는 슬롯은 미리보기를 만들 수 없다(배치를 시도하면 UnitPlacer가 알린다).
        if (!finder.TryResolveSlot(out Placeable slot, out PlaceData data) || slot.prefab == null)
        {
            ghost.HideGhost();
            return;
        }

        ghost.ShowGhost(slot.prefab, data);
    }

    private bool IsPlacing()
    {
        return !input.Blocked && palette.Mode == PlaceMode.Place;
    }

    // 재배치 모드면 재배치 입력을, 아니면 현재 모드의 기능을 실행한다.
    private void OnPress()
    {
        dragDetect.MarkPress();
        Tile tile = pointerPick.UnderPointer();
        rangeStore.KeepRange(rangeCalc.GetRange(tile));

        if (palette.Mode == PlaceMode.Replace)
        {
            ReplacePress(tile);
            return;
        }

        if (tile == null)
        {
            action.skillCast?.ClearSelection(); // 타일 밖 클릭 = 스킬 시전 취소
            buildingUi.CloseUnlessOverUi();
            return;
        }

        if (!buildingUi.OpenIfBuilding(tile, palette.Mode))
        {
            return;
        }

        dispatch[palette.Mode](tile);
    }

    // 제자리 우클릭(뗄 때까지 거의 안 움직임)이면 스킬 시전 취소. 우클릭 드래그는 카메라 팬/회전이라 무시한다.
    private void OnRightPress()
    {
        rightDragDetect.MarkPress();
    }

    private void OnRightRelease()
    {
        if (rightDragDetect.MovedEnough()) return;
        action.skillCast?.ClearSelection();
    }

    private void ReplacePress(Tile tile)
    {
        if (replace.IsHolding)
        {
            if (TryHeldData(out PlaceData data))
            {
                action.Drop(data);
            }
            return;
        }

        if (tile == null)
        {
            buildingUi.CloseUnlessOverUi();
            return;
        }

        if (tile.HasUnit)
        {
            action.PickUpUnit(tile);
            return;
        }

        action.SelectTile(tile);
    }

    // 떼는 순간: 집은 채 드래그였다면 목표 타일에 내려놓는다(제자리 클릭이면 집은 채 유지).
    private void OnRelease()
    {
        if (palette.Mode != PlaceMode.Replace)
        {
            return;
        }

        if (!replace.IsHolding)
        {
            return;
        }

        if (!IsDrag(pointerPick.UnderPointer()))
        {
            return;
        }

        if (TryHeldData(out PlaceData data))
        {
            action.Drop(data);
        }
    }

    // 집은 유닛 프리뷰가 목표 자리 한가운데를 따라가게 한다.
    private void FollowHeld()
    {
        if (palette.Mode != PlaceMode.Replace)
        {
            return;
        }

        if (!replace.IsHolding)
        {
            return;
        }

        if (TryHeldData(out PlaceData data))
        {
            replace.MoveHeldTo(data);
        }
    }

    // 집은 유닛이 지금 포인터 위치에 놓인다면 어떻게 놓일지(집기 전 종류·크기를 그대로 쓴다).
    private bool TryHeldData(out PlaceData data)
    {
        return finder.TryResolve(replace.HeldKind, replace.HeldSize, out data);
    }

    // 다른 타일 위에서 뗐거나 화면상 충분히 움직였으면 드래그로 본다(제자리 클릭과 구분).
    private bool IsDrag(Tile releaseTile)
    {
        if (releaseTile != null && releaseTile != replace.HeldFromTile)
        {
            return true;
        }

        return dragDetect.MovedEnough();
    }
}
