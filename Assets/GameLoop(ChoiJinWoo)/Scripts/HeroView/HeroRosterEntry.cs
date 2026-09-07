using System;
using UnityEngine;

public enum HeroRosterState
{
    Available,
    Placed
}

public class HeroRosterEntry
{
    public readonly Guid Id;
    public readonly Placeable Slot;
    public readonly HeroData Data;
    public readonly int CitizenCost;
    public HeroRosterState State { get; private set; } = HeroRosterState.Available;
    public GameObject PlacedUnit { get; private set; }
    public bool IsNew { get; private set; } = true;

    public Sprite Icon => Data.Icon;
    public int Tier => Data.Tier;

    public HeroRosterEntry(Placeable slot, HeroData data, int citizenCost = 0)
    {
        Id = Guid.NewGuid();
        Slot = slot;
        Data = data;
        CitizenCost = citizenCost;
    }

    public HeroRosterEntry(Guid savedId, Placeable slot, HeroData data, int citizenCost)
    {
        Id = savedId;
        Slot = slot;
        Data = data;
        CitizenCost = citizenCost;
        IsNew = false;
    }

    public void MarkSeen()
    {
        IsNew = false;
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

    public bool TryGetMergeKey(out MergeKey key)
    {
        key = Data.MergeKey;
        return true;
    }
}
