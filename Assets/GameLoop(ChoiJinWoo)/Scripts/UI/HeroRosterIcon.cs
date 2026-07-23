using System;
using UnityEngine;
using UnityEngine.UI;

// 로스터 아이콘 한 칸. HeroRosterPanel이 엔트리 하나당 하나씩 인스턴스화해서 값을 채운다.
public class HeroRosterIcon : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Sprite placedIcon;
    [SerializeField] private Button button;

    private HeroRosterEntry entry;
    private Action<HeroRosterEntry> onClick;

    public void Set(HeroRosterEntry entry, Action<HeroRosterEntry> onClick)
    {
        this.entry = entry;
        this.onClick = onClick;

        icon.sprite = entry.State == HeroRosterState.Placed ? placedIcon : entry.Slot.icon;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => this.onClick?.Invoke(this.entry));
    }
}
