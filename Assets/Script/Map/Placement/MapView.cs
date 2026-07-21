using System.Collections.Generic;
using System.Resources;
using UnityEngine;

// UI가 읽을 맵 상태(선택 타일·상태 문구·현재 모드·사거리)를 보관하고 내준다.
public class MapView : MonoBehaviour
{
    [SerializeField] private MapInput input;
    [SerializeField] private PlacePalette palette;

    // MapCommand가 조립할 때 넣어준다
    public PointerPick pointerPick;
    public UnitReplace replace;
    public RangeInfo rangeInfo;
    public CitizenManager citizenManager;
    public ResourcesManager resourcesManager;

    private readonly TileSelect tileSelect = new();
    private readonly StatusText statusText = new();

    // ---- 상태 기록(PlaceAction이 결과를 알릴 때 부른다) ----

    public void Select(Tile tile) { tileSelect.Select(tile); }
    public void ClearSelection() { tileSelect.Clear(); }
    public bool IsSelected(Tile tile) { return tileSelect.IsSelected(tile); }
    public void SetStatus(string value) { statusText.Set(value); }

    // ---- 읽기(PanelLogic·TilePaintView가 본다) ----

    public bool IsHolding { get { return replace.IsHolding; } }
    public bool InputBlocked { get { return input.Blocked; } }
    public Tile HoverTile { get { return pointerPick.UnderPointer(); } }
    public bool IsPlacing { get { return palette.Mode == PlaceMode.Place; } }
    public OccupantKind PlacingKind { get { return palette.CurrentSlot().kind; } }
    public string PlacingLabel { get { return palette.CurrentSlot().label; } }
    public int PlacingRange { get { return palette.PreviewRange(); } }
    public string Status { get { return statusText.Text; } }
    public Tile Selected { get { return tileSelect.Selected; } }
    public string Mode { get { return palette.Mode.ToString(); } }
    public IReadOnlyList<Placeable> Items { get { return palette.Slots; } }

    public int UnitIndex
    {
        get
        {
            if (palette.Mode != PlaceMode.Place) return -1;
            return palette.CurrentIndex;
        }
    }

    public int UnitRange(GameObject unit)
    {
        return rangeInfo.Of(unit);
    }

    // ---- 모드 전환(PanelLogic 버튼이 부른다) ----

    public void SetUnit(int index) { palette.SelectSlot(index); }
    public void SetUnit(string label) { palette.SelectSlot(label); }
    public void SetReplace()
    {
        palette.SelectReplace();
    }
    public void SetRemove() { palette.SelectRemove(); }
    public void ClearMode() { palette.ClearMode(); }
    public void SetBlock(bool value) { input.SetBlock(value); }

    public bool CheckCanBuild(string label)
    {
        var slot = palette.GetSlot(label);

        switch (slot.kind)
        {
            case OccupantKind.None:
                return false;
            case OccupantKind.MeleeHero:
                if (citizenManager.CheckCanUseCitizen(slot.prefab.GetComponent<Hero>().CitizenAmount))
                    return true;
                else
                    return false;
            case OccupantKind.RangedHero:
                if (citizenManager.CheckCanUseCitizen(slot.prefab.GetComponent<Hero>().CitizenAmount))
                    return true;
                else
                    return false;
            case OccupantKind.Building:
                if (resourcesManager.CheckResources(slot.prefab.GetComponent<House>().Resources))
                    return true;
                else
                    return false;
            case OccupantKind.Resource:
                if (resourcesManager.CheckResources(slot.prefab.GetComponent<ProductionFacility>().BasicValue.ConstructProduct))
                    return true;
                else
                    return false;
            default:
                return false;
        }
    }
}
