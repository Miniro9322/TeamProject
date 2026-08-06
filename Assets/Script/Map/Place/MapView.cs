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
    public RangeInfo rangeInfo;
    public CitizenManager citizenManager;
    public ResourcesManager resourcesManager;

    private UnitReplace _replace;
    // MapAssemble이 대입하는 시점에 held 변경 이벤트를 걸어준다.
    public UnitReplace replace
    {
        get { return _replace; }
        set
        {
            if (_replace != null) _replace.OnHoldChanged -= HandleHoldChanged;
            _replace = value;
            if (_replace != null) _replace.OnHoldChanged += HandleHoldChanged;
        }
    }

    private readonly SelectedTileData tileSelect = new();

    private void OnEnable()
    {
        palette.OnModeChanged += HandleModeChanged;
    }

    private void OnDisable()
    {
        palette.OnModeChanged -= HandleModeChanged;
    }

    // 모드·집은 상태 중 하나라도 바뀌면 UI가 폴링 없이 갱신할 수 있게 알린다.
    public event Action OnStateChanged;

    private void HandleModeChanged()
    {
        // 재배치 모드에서 집은 채로 다른 모드로 빠지면(취소) 원래 자리로 돌려놓는다.
        if (!IsReplacing && _replace != null && _replace.IsHolding)
        {
            _replace.ReturnHeld();
        }

        OnStateChanged?.Invoke();
        if (IsOff) OnOffMode?.Invoke();
    }

    private void HandleHoldChanged()
    {
        OnStateChanged?.Invoke();
    }

    // ---- 상태 기록(PlaceAction이 결과를 알릴 때 부른다) ----

    public void Select(Tile tile) { tileSelect.Select(tile); }
    public void ClearSelection() { tileSelect.Clear(); }
    public bool IsSelected(Tile tile) { return tileSelect.IsSelected(tile); }

    // ---- 읽기(TilePaintView가 본다) ----

    public bool IsHolding { get { return _replace != null && _replace.IsHolding; } }
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
    public bool IsRemoving { get { return palette.Mode == PlaceMode.Remove; } }
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

    // ---- 모드 전환(UI 버튼이 부른다) ----

    public void SetUnit(int index)  => palette.SelectSlot(index); 
    public void SetUnit(string label) => palette.SelectSlot(label); 
    public Placeable GetSlot(string label) => palette.GetSlot(label);
    public void SetHero(HeroRosterEntry entry) => palette.SelectRuntimeSlot(entry);
    public void SetHeroCreate(Placeable slot) => palette.SelectHeroCreate(slot);
    public void SetReplace()
    {
        palette.SelectReplace();
    }
    public void SetRemove() => palette.SelectRemove();
    public void ClearMode()
    {
        palette.ClearMode();
    }

    // 재배치 모드는 유지한 채, 집은 유닛만 원래 자리로 되돌린다(집기 취소).
    public void CancelHold()
    {
        if (_replace != null && _replace.IsHolding)
        {
            _replace.ReturnHeld();
        }
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
