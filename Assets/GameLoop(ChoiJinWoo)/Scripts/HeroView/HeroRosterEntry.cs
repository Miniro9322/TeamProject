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
    public HeroRosterState State { get; private set; } = HeroRosterState.Available;
    public GameObject PlacedUnit { get; private set; }

    // 제거될 때 파괴되는 배치 인스턴스에서 건져낸 강화 진행도. 재배치 시 새 인스턴스에 다시 적용된다.
    public int SkillLevel { get; private set; }
    public int StatLevel { get; private set; }

    public HeroRosterEntry(Placeable slot)
    {
        Id = Guid.NewGuid();
        Slot = slot;
    }

    public void MarkPlaced(GameObject unit)
    {
        State = HeroRosterState.Placed;
        PlacedUnit = unit;
    }

    // 파괴되기 전의 강화 진행도를 보관해둔다.
    public void SaveUpgradeState(int skillLevel, int statLevel)
    {
        SkillLevel = skillLevel;
        StatLevel = statLevel;
    }

    public void MarkAvailable()
    {
        State = HeroRosterState.Available;
        PlacedUnit = null;
    }
}
