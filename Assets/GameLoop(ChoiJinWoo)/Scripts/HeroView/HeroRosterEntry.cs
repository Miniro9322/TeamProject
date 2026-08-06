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

    // 배치 여부와 무관하게 이 엔트리의 MergeKey를 구한다. 배치돼 있으면 실제 인스턴스에서,
    // 아니면 슬롯의 프리팹 에셋에서 읽는다. 프리팹 참조가 끊긴 경우(Placeable.prefab == null 등)
    // 를 조용히 실패로 처리해 합성 인덱싱이 로스터 갱신 이벤트를 죽이지 않게 한다.
    public bool TryGetMergeKey(out MergeKey key)
    {
        GameObject source = PlacedUnit != null ? PlacedUnit : Slot?.prefab;
        if (source != null && source.TryGetComponent(out Hero hero))
        {
            key = hero.MergeKey;
            return true;
        }

        key = default;
        return false;
    }
}
