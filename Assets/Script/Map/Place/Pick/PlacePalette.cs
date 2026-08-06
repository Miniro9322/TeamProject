using System;
using System.Collections.Generic;
using UnityEngine;

// 배치할 슬롯 목록과 현재 선택(인덱스·모드)을 보관한다. 목록은 인스펙터에서 편집.
public class PlacePalette : MonoBehaviour
{
    [SerializeField] private List<Placeable> _slots = new();
    private int _index;
    private PlaceMode _mode = PlaceMode.Off;
    private HeroRosterEntry _runtimeEntry;
    // "생성" 버튼이 배치 모드만 열어둘 때 쓴다. 로스터 등록 전이라 엔트리가 없다.
    private Placeable _pendingCreateSlot;

    // 모드가 바뀔 때마다 알린다(UI가 매 프레임 폴링하지 않게).
    public event Action OnModeChanged;

    public PlaceMode Mode
    {
        get { return _mode; }
    }

    public IReadOnlyList<Placeable> Slots
    {
        get { return _slots; }
    }

    public int CurrentIndex
    {
        get { return _index; }
    }

    public HeroRosterEntry CurrentRuntimeEntry
    {
        get { return _runtimeEntry; }
    }

    // 배치 성공 시 PlaceAction이 실제 생성(비용 차감·로스터 등록)을 할지 판단하는 데 쓴다.
    public Placeable PendingCreateSlot
    {
        get { return _pendingCreateSlot; }
    }

    // 현재 슬롯을 돌려준다(인덱스가 유효하다는 가정). 검사는 TryCurrentSlot이 한다.
    public Placeable CurrentSlot()
    {
        if (_runtimeEntry != null) return _runtimeEntry.Slot;
        if (_pendingCreateSlot != null) return _pendingCreateSlot;
        return _slots[_index];
    }

    // 현재 인덱스가 유효하면 슬롯을 내주고 true(검사 담당 게이트).
    public bool TryCurrentSlot(out Placeable slot)
    {
        if (_runtimeEntry != null) { slot = _runtimeEntry.Slot; return true; }
        if (_pendingCreateSlot != null) { slot = _pendingCreateSlot; return true; }
        if (_index < 0 || _index >= _slots.Count) { slot = null; return false; }
        slot = _slots[_index];
        return true;
    }

    public void SelectSlot(int index)
    {
        _index = index;
        _runtimeEntry = null;
        _pendingCreateSlot = null;
        SetMode(PlaceMode.Place);
    }

    public void SelectSlot(string label)
    {
        foreach(var slot in _slots)
        {
            if(slot.label == label)
            {
                _index = _slots.IndexOf(slot);
                _runtimeEntry = null;
                _pendingCreateSlot = null;
                SetMode(PlaceMode.Place);
                break;
            }
        }
    }

    // 로스터 엔트리를 현재 배치 대상으로 선택한다(이미 생성된 영웅의 재배치).
    public void SelectRuntimeSlot(HeroRosterEntry entry)
    {
        _runtimeEntry = entry;
        _pendingCreateSlot = null;
        SetMode(PlaceMode.Place);
    }

    // "생성" 버튼: 배치 모드만 열어둔다. 실제 생성은 PlaceAction이 배치에 성공했을 때 한다.
    public void SelectHeroCreate(Placeable slot)
    {
        _runtimeEntry = null;
        _pendingCreateSlot = slot;
        SetMode(PlaceMode.Place);
    }

    public Placeable GetSlot(string label)
    {
        foreach (var slot in _slots)
        {
            if (slot.label == label)
            {
                return slot;
            }
        }

        return null;
    }
    public void SelectReplace()
    {
        _runtimeEntry = null;
        _pendingCreateSlot = null;
        SetMode(PlaceMode.Replace);
    }

    public void SelectRemove()
    {
        _runtimeEntry = null;
        _pendingCreateSlot = null;
        SetMode(PlaceMode.Remove);
    }

    public void ClearMode()
    {
        _runtimeEntry = null;
        _pendingCreateSlot = null;
        SetMode(PlaceMode.Off);
    }

    private void SetMode(PlaceMode mode)
    {
        _mode = mode;
        OnModeChanged?.Invoke();
    }
}
