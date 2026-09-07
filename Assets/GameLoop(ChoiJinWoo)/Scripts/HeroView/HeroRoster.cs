using System;
using System.Collections.Generic;

public class HeroRoster
{
    private readonly List<HeroRosterEntry> _entries = new();

    public event Action Changed;

    public IReadOnlyList<HeroRosterEntry> Entries
    {
        get { return _entries; }
    }

    public HeroRosterEntry Add(Placeable slot, HeroData data, int citizenCost = 0)
    {
        HeroRosterEntry entry = new HeroRosterEntry(slot, data, citizenCost);
        _entries.Insert(0, entry);
        Changed?.Invoke();
        return entry;
    }

    public HeroRosterEntry AddRestored(Guid savedId, Placeable slot, HeroData data, int citizenCost)
    {
        HeroRosterEntry entry = new HeroRosterEntry(savedId, slot, data, citizenCost);
        _entries.Add(entry);
        Changed?.Invoke();
        return entry;
    }

    public bool Remove(HeroRosterEntry entry)
    {
        bool removed = _entries.Remove(entry);
        if (removed) Changed?.Invoke();
        return removed;
    }

    public void NotifyStateChanged()
    {
        Changed?.Invoke();
    }

    public void MarkAllSeen()
    {
        foreach (HeroRosterEntry entry in _entries)
        {
            entry.MarkSeen();
        }
    }

    public bool Contains(HeroRosterEntry entry)
    {
        return _entries.Contains(entry);
    }
}
