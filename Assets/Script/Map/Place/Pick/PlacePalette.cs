using System;
using UnityEngine;

// 현재 배치 대상(로스터 엔트리)과 모드를 보관한다.
public class PlacePalette : MonoBehaviour
{
    private PlaceMode _mode = PlaceMode.Off;
    private HeroRosterEntry _runtimeEntry;

    // 모드가 바뀔 때마다 알린다(UI가 매 프레임 폴링하지 않게).
    public event Action OnModeChanged;

    public PlaceMode Mode
    {
        get { return _mode; }
    }

    public HeroRosterEntry CurrentRuntimeEntry
    {
        get { return _runtimeEntry; }
    }

    // 현재 슬롯을 돌려준다(엔트리가 있다는 가정). 검사는 TryCurrentSlot이 한다.
    public Placeable CurrentSlot()
    {
        return _runtimeEntry.Slot;
    }

    // 배치 대상 엔트리가 있으면 슬롯을 내주고 true(검사 담당 게이트).
    public bool TryCurrentSlot(out Placeable slot)
    {
        if (_runtimeEntry == null) { slot = null; return false; }
        slot = _runtimeEntry.Slot;
        return true;
    }

    // 로스터 엔트리를 현재 배치 대상으로 선택한다(생성 직후 또는 이미 생성된 영웅의 재배치).
    public void SelectRuntimeSlot(HeroRosterEntry entry)
    {
        _runtimeEntry = entry;
        SetMode(PlaceMode.Place);
    }

    public void SelectReplace()
    {
        _runtimeEntry = null;
        SetMode(PlaceMode.Replace);
    }

    public void SelectRemove()
    {
        _runtimeEntry = null;
        SetMode(PlaceMode.Remove);
    }

    public void ClearMode()
    {
        _runtimeEntry = null;
        SetMode(PlaceMode.Off);
    }

    private void SetMode(PlaceMode mode)
    {
        _mode = mode;
        OnModeChanged?.Invoke();
    }
}
