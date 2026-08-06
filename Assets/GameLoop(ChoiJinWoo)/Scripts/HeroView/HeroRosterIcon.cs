using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 로스터 아이콘 한 칸. HeroRosterPanel이 엔트리 하나당 하나씩 인스턴스화해서 값을 채운다.
public class HeroRosterIcon : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI statLevelText;
    [SerializeField] private TextMeshProUGUI skillLevelText;

    private HeroRosterEntry entry;
    private Action<HeroRosterEntry, HeroRosterIcon> onClick;
    private Action<HeroRosterEntry> onDoubleClick;

    public void Set(HeroRosterEntry entry, Action<HeroRosterEntry, HeroRosterIcon> onClick, Action<HeroRosterEntry> onDoubleClick = null)
    {
        this.entry = entry;
        this.onClick = onClick;
        this.onDoubleClick = onDoubleClick;

        icon.sprite = entry.State == HeroRosterState.Placed ? entry.Slot.placedIcon : entry.Slot.icon;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => this.onClick?.Invoke(this.entry, this));
    }

    // 배치된 영웅 아이콘을 빠르게 두 번 클릭하면 합성 시도로 본다.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.clickCount >= 2) onDoubleClick?.Invoke(entry);
    }

    public void UpdateLevel(int statLevel, int skillLevel)
    {
        statLevelText.text = $"LV.{statLevel}";
        skillLevelText.text = $"LV.{skillLevel}";
    }
}
