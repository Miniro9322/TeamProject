using UnityEngine;

// 배치된 유닛의 사거리를 장부에서 읽어 준다(음수면 0).
public class RangeInfo
{
    private readonly UnitList _unitList;

    public RangeInfo(UnitList unitList)
    {
        _unitList = unitList;
    }

    public int RangeOf(GameObject unit)
    {
        _unitList.TryGetRange(unit, out int range);
        return Mathf.Max(0, range);
    }
}
