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
    public UnitReplace replace;
    public BuildingUiLink buildingUi;
    public PlaceAction action;
    public Dictionary<PlaceMode, Action<Tile>> dispatch;
    public float placeYOffset;

    private void OnEnable()
    {
        input.Pressed += OnPress;
        input.Released += OnRelease;
    }

    private void OnDisable()
    {
        input.Pressed -= OnPress;
        input.Released -= OnRelease;
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
            buildingUi.CloseUnlessOverUi();
            return;
        }

        if (!buildingUi.OpenIfBuilding(tile, palette.Mode))
        {
            return;
        }

        dispatch[palette.Mode](tile);
    }

    private void ReplacePress(Tile tile)
    {
        if (replace.IsHolding)
        {
            Tile target = pointerPick.NearestCell();
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

        Tile target = pointerPick.NearestCell();
        if (target != null)
        {
            action.Drop(target);
        }
    }

    // 집은 유닛 프리뷰가 목표 타일 윗면을 따라가게 한다.
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

        Tile target = pointerPick.NearestCell();
        if (target != null)
        {
            replace.MoveHeldTo(target, placeYOffset);
        }
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
