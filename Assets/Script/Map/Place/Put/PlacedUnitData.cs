using System.Collections.Generic;
using UnityEngine;

// 배치된 유닛 장부. 지금 판에 올라와 있는 유닛과 그 유닛의 사거리를 기억한다.
// MapGame(중재자)이 배치/제거/재배치 때 이 장부에 넣고·빼고·물어본다.
public class PlacedUnitData
{
    private readonly Dictionary<GameObject, int> _attackRangeByUnit = new();

    // 유닛 하나를 장부에 올린다(사거리와 함께).
    public void Add(GameObject unit, int attackRange)
    {
        _attackRangeByUnit[unit] = attackRange;
    }

    // 유닛 하나를 장부에서 내린다.
    public void Remove(GameObject unit)
    {
        _attackRangeByUnit.Remove(unit);
    }

    // 장부를 통째로 비운다.
    public void Clear()
    {
        _attackRangeByUnit.Clear();
    }

    // 유닛의 사거리를 꺼낸다. 장부에 없으면 false.
    public bool TryGetRange(GameObject unit, out int attackRange)
    {
        return _attackRangeByUnit.TryGetValue(unit, out attackRange);
    }
}
