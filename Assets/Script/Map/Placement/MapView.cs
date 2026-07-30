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

    private readonly TileSelect tileSelect = new();

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
    public OccupantKind PlacingKind { get { return palette.CurrentSlot().kind; } }
    // 지금 배치하려는 것이 차지하는 칸 수. 생산 건물만 자기 데이터(ProductionValue)에서 크기를 들고 오고,
    // 나머지는 한 칸이다. 크기의 원본은 SO 하나뿐이라 슬롯·씬마다 갈리지 않는다.
    public Vector2Int PlacingSize
    {
        get
        {
            Placeable slot = palette.CurrentSlot();
            if (slot == null || slot.prefab == null)
            {
                return Vector2Int.one;
            }

            if (slot.prefab.TryGetComponent(out ProductionFacility facility) && facility.BasicValue != null)
            {
                return facility.BasicValue.TileSize;
            }

            return Vector2Int.one;
        }
    }

    // 지금 배치하려는 것이 포인터 위치에서 덮게 될 자리(프리뷰가 읽는다).
    public PlacementArea HoverArea
    {
        get
        {
            if (pointerPick == null)
            {
                return null;
            }

            return pointerPick.GetArea(PlacingSize);
        }
    }

    public PlacementArea HeldArea
    {
        get
        {
            if (pointerPick == null || replace == null || !replace.IsHolding)
            {
                return null;
            }

            return pointerPick.GetArea(replace.HeldSize);
        }
    }

    // 지금 배치하려는 것의 프리팹(미리보기가 읽는다). 슬롯이 비면 null.
    public GameObject PlacingPrefab
    {
        get
        {
            if (palette.TryCurrentSlot(out Placeable slot))
            {
                return slot.prefab;
            }

            return null;
        }
    }

    public string PlacingLabel { get { return palette.CurrentSlot().label; } }
    public Tile Selected { get { return tileSelect.Selected; } }
    public string Mode { get { return palette.Mode.ToString(); } }
    public IReadOnlyList<Placeable> Items { get { return palette.Slots; } }

    public event Action OnOffMode;

    public int UnitIndex
    {
        get
        {
            if (palette.Mode != PlaceMode.Place)
            {
                return -1;
            }

            return palette.CurrentIndex;
        }
    }

    public bool TryRange(
        GameObject unit,
        out int range,
        out RangeShape shape)
    {
        return rangeInfo.TryGet(unit, out range, out shape);
    }

    // ---- 모드 전환(PanelLogic 버튼이 부른다) ----

    public void SetUnit(int index) { palette.SelectSlot(index); }
    public void SetUnit(string label) { palette.SelectSlot(label); }
    public Placeable GetSlot(string label) { return palette.GetSlot(label); }
    public void SetHero(HeroRosterEntry entry) { palette.SelectRuntimeSlot(entry); }
    public void SetReplace()
    {
        palette.SelectReplace();
    }
    public void SetRemove() { palette.SelectRemove(); }
    public void ClearMode() { palette.ClearMode(); OnOffMode?.Invoke(); }
    public void SetBlock(bool value) { input.SetBlock(value); }

    public bool CheckCanBuild(string label)
    {
        var slot = palette.GetSlot(label);

        switch (slot.kind)
        {
            case OccupantKind.None:
                return false;
            case OccupantKind.MeleeHero:
            case OccupantKind.RangedHero:
            {
                Hero hero = slot.prefab.GetComponent<Hero>();
                var cost = hero.Cost;
                return citizenManager.CheckCanUseCitizen(hero.CitizenAmount) && cost != null && resourcesManager.CheckResources(cost);
            }
            case OccupantKind.Building:
                if (resourcesManager.CheckResources(slot.prefab.GetComponent<House>().Resources))
                    return true;
                else
                    return false;
            case OccupantKind.Resource:
                if (resourcesManager.CheckResources(slot.prefab.GetComponent<ProductionFacility>().GetConstructCost()))
                    return true;
                else
                    return false;
            default:
                return false;
        }
    }
}
