using System;
using System.Collections.Generic;
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

    private readonly SelectedTileData tileSelect = new();

    // ---- 상태 기록(PlaceAction이 결과를 알릴 때 부른다) ----

    public void Select(Tile tile) { tileSelect.Select(tile); }
    public void ClearSelection() { tileSelect.Clear(); }
    public bool IsSelected(Tile tile) { return tileSelect.IsSelected(tile); }

    // ---- 읽기(PanelLogic·TilePaintView가 본다) ----

    public bool IsHolding { get { return replace.IsHolding; } }
    public bool InputBlocked { get { return input.Blocked; } }
    public GameObject HeldUnit { get { return replace.HeldUnit; } }
    public OccupantKind HeldKind { get { return replace.HeldKind; } }
    public Tile HoverTile
    {
        get
        {
            if (pointerPick == null)
            {
                return null;
            }

            return pointerPick.UnderPointer();
        }
    }
    public bool IsPlacing { get { return palette.Mode == PlaceMode.Place; } }
    public bool IsReplacing { get { return palette.Mode == PlaceMode.Replace; } }
    public bool IsOff { get { return palette.Mode == PlaceMode.Off; } }
    public Vector2Int HeldSize { get { return replace.HeldSize; } }

    public string PlacingLabel { get { return palette.CurrentSlot().label; } }
    public Tile Selected { get { return tileSelect.Selected; } }
    public string Mode { get { return palette.Mode.ToString(); } }
    public IReadOnlyList<Placeable> Items { get { return palette.Slots; } }

    public event Action OnOffMode;

    public int UnitIndex => palette.CurrentIndex;

    public bool TryRange(
        GameObject unit,
        out int range,
        out RangeShape shape)
    {
        return rangeInfo.TryGet(unit, out range, out shape);
    }

    // ---- 모드 전환(PanelLogic 버튼이 부른다) ----

    public void SetUnit(int index)  => palette.SelectSlot(index); 
    public void SetUnit(string label) => palette.SelectSlot(label); 
    public Placeable GetSlot(string label) => palette.GetSlot(label);
    public void SetHero(HeroRosterEntry entry) => palette.SelectRuntimeSlot(entry); 
    public void SetReplace()
    {
        palette.SelectReplace();
    }
    public void SetRemove() => palette.SelectRemove();
    public void ClearMode() 
    {
        palette.ClearMode(); 
        OnOffMode?.Invoke(); 
    }
    public void SetBlock(bool value) => input.SetBlock(value);

    // 이 슬롯을 지금 놓을 여유가 있는지(UI 버튼 활성화용). 자리가 되는지는 보지 않는다.
    public bool CheckCanBuild(string label)
    {
        if (!PlaceCost.TryGet(palette.GetSlot(label), out PlaceCost cost))
        {
            return false;
        }

        return citizenManager.CheckCanUseCitizen(cost.Citizens)
            && resourcesManager.CheckResources(cost.Resources);
    }
}
