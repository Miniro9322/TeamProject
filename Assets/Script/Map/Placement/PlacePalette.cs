using System.Collections.Generic;
using UnityEngine;

// 배치할 슬롯 목록과 현재 선택(인덱스·모드)을 보관한다. 목록은 인스펙터에서 편집.
public class PlacePalette : MonoBehaviour
{
    [SerializeField] private List<Placeable> _slots = new();
    private int _index;
    private PlaceMode _mode = PlaceMode.Off;

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

    // 현재 슬롯을 돌려준다(인덱스가 유효하다는 가정). 검사는 TryCurrentSlot이 한다.
    public Placeable CurrentSlot()
    {
        return _slots[_index];
    }

    // 현재 인덱스가 유효하면 슬롯을 내주고 true(검사 담당 게이트).
    public bool TryCurrentSlot(out Placeable slot)
    {
        if (_index < 0 || _index >= _slots.Count) { slot = null; return false; }
        slot = _slots[_index];
        return true;
    }

    // 현재 슬롯의 미리보기 사거리(음수면 0).
    public int PreviewRange()
    {
        return Mathf.Max(0, CurrentSlot().attackRange);
    }

    public void SelectSlot(int index)
    {
        _index = index;
        _mode = PlaceMode.Place;
    }

    public void SelectSlot(string label)
    {
        foreach(var slot in _slots)
        {
            if(slot.label == label)
            {
                _index = _slots.IndexOf(slot);
                _mode = PlaceMode.Place;
                break;
            }
        }
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
        _mode = PlaceMode.Replace;
    }

    public void SelectRemove()
    {
        _mode = PlaceMode.Remove;
    }

    public void ClearMode()
    {
        _mode = PlaceMode.Off;
    }
}
