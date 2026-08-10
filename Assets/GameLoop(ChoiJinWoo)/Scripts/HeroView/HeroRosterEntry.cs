using System;
using UnityEngine;

public enum HeroRosterState
{
    Available,  // 보유 중, 배치 가능
    Placed      // 맵에 배치되어 있음
}

// 로스터 항목 하나. 같은 슬롯(같은 프리팹)이라도 개체를 구분해야 하므로 고유 Id를 갖는다.
public class HeroRosterEntry
{
    public readonly Guid Id;
    public readonly Placeable Slot;
    public readonly HeroData Data;
    public HeroRosterState State { get; private set; } = HeroRosterState.Available;
    public GameObject PlacedUnit { get; private set; }

    public Sprite Icon => Data.Icon;
    public int Tier => Data.Tier;

    public HeroRosterEntry(Placeable slot, HeroData data)
    {
        Id = Guid.NewGuid();
        Slot = slot;
        Data = data;
    }

    public void MarkPlaced(GameObject unit)
    {
        State = HeroRosterState.Placed;
        PlacedUnit = unit;
    }

    public void MarkAvailable()
    {
        State = HeroRosterState.Available;
        PlacedUnit = null;
    }

    // 이 엔트리의 MergeKey. HeroData에서 바로 나오므로 배치 여부와 무관하게 항상 구할 수 있다.
    public bool TryGetMergeKey(out MergeKey key)
    {
        key = Data.MergeKey;
        return true;
    }
}
