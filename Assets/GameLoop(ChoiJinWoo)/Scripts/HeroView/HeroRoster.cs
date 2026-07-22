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

    public HeroRosterEntry Add(Placeable slot)
    {
        HeroRosterEntry entry = new HeroRosterEntry(slot);
        _entries.Add(entry);
        Changed?.Invoke();
        return entry;
    }

    // 배치/제거로 엔트리 상태만 바뀌었을 때 UI 갱신용
    public void NotifyStateChanged()
    {
        Changed?.Invoke();
    }
}
