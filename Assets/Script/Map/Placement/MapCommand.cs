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
    public Dictionary<PlaceMode, Action<Tile>> dispatch;
    public float placeYOffset;

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
    }

    // 재배치 모드면 재배치 입력을, 아니면 현재 모드의 기능을 실행한다.
    private void OnPress()
    {
        dragDetect.MarkPress();
        Tile tile = pointerPick.UnderPointer();

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
            PlacementArea target = HeldArea();
            if (target != null)
            {
                action.Drop(target);
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

        PlacementArea target = HeldArea();
        if (target != null)
        {
            action.Drop(target);
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

        PlacementArea target = HeldArea();
        if (target != null)
        {
            replace.MoveHeldTo(target, placeYOffset);
        }
    }

    // 집은 유닛이 지금 포인터 위치에 놓인다면 덮게 될 자리(집기 전 크기를 그대로 쓴다).
    private PlacementArea HeldArea()
    {
        return pointerPick.GetArea(replace.HeldSize);
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
