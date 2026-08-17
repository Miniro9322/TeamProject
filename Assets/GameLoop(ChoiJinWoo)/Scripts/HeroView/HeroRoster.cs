using System;
using System.Collections.Generic;

// 생성된 영웅 엔트리 전체를 담는다. HeroSetPanel이 채우고, HeroRosterPanel이 보여준다.
// 배치/제거는 엔트리를 목록에서 빼거나 넣지 않고 상태(Available/Placed)만 바꾼다.
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
        _entries.Add(entry);
        Changed?.Invoke();
        return entry;
    }

    // 합성 등으로 개체 자체가 소멸할 때 씀 — Available로 되돌리는 MarkAvailable과 달리 엔트리를 목록에서 아예 뺀다.
    public bool Remove(HeroRosterEntry entry)
    {
        bool removed = _entries.Remove(entry);
        if (removed) Changed?.Invoke();
        return removed;
    }

    // 배치/제거로 엔트리 상태만 바뀌었을 때 UI 갱신용
    public void NotifyStateChanged()
    {
        Changed?.Invoke();
    }

    public bool Contains(HeroRosterEntry entry)
    {
        return _entries.Contains(entry);
    }
}
