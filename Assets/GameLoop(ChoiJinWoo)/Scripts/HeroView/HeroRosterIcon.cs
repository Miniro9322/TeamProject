using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 로스터 아이콘 한 칸. HeroRosterPanel이 엔트리 하나당 하나씩 인스턴스화해서 값을 채운다.
public class HeroRosterIcon : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private Button button;
    [SerializeField] private Image placedIcon;
    [SerializeField] private TextMeshProUGUI tierText;

    private const float DoubleClickWindow = 0.3f; // PlaceAction.DoubleClickWindow와 동일한 값

    private HeroRosterEntry entry;
    private Action<HeroRosterEntry, HeroRosterIcon> onClick;
    private Action<HeroRosterEntry> onDoubleClick;
    private Coroutine pendingSingleClick;

    public void Set(HeroRosterEntry entry, Action<HeroRosterEntry, HeroRosterIcon> onClick, Action<HeroRosterEntry> onDoubleClick = null)
    {
        this.entry = entry;
        this.onClick = onClick;
        this.onDoubleClick = onDoubleClick;

        icon.sprite = entry.State == HeroRosterState.Placed ? entry.Slot.placedIcon : entry.Slot.icon;
        float alpha = entry.State == HeroRosterState.Placed ? 1f : 0f;
        Color c = placedIcon.color;
        c.a = alpha;
        placedIcon.color = c;
        tierText.text = entry.PlacedUnit.GetComponent<Hero>().Tier.ToString();
    }

    // 첫 클릭은 곧바로 실행하지 않고 잠깐 기다린다. 그 안에 두 번째 클릭이 오면 단일 클릭 동작은
    // 취소되고 더블클릭 동작만 실행된다 — 더블클릭에 단일 클릭 동작(배치모드 진입 등)이 같이
    // 발동하지 않도록 하기 위함.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.clickCount >= 2)
        {
            if (pendingSingleClick != null)
            {
                StopCoroutine(pendingSingleClick);
                pendingSingleClick = null;
            }
            onDoubleClick?.Invoke(entry);
            return;
        }

        if (pendingSingleClick != null) StopCoroutine(pendingSingleClick);
        pendingSingleClick = StartCoroutine(FireSingleClickAfterDelay());
    }

    private IEnumerator FireSingleClickAfterDelay()
    {
        yield return new WaitForSeconds(DoubleClickWindow);
        pendingSingleClick = null;
        onClick?.Invoke(entry, this);
    }
}
