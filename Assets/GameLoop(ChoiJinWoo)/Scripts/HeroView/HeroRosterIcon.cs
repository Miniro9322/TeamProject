using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private List<Sprite> classIcons;
    [SerializeField] private Image classIcon;
    [SerializeField] private TooltipTrigger toolTip;
    [SerializeField] private Slider hpSlider;

    private const float DoubleClickWindow = 0.2f; // PlaceAction.DoubleClickWindow와 동일한 값

    private HeroRosterEntry entry;
    private Action<HeroRosterEntry, HeroRosterIcon> onClick;
    private Action<HeroRosterEntry> onDoubleClick;
    private Coroutine pendingSingleClick;

    public void Set(HeroRosterEntry entry, Action<HeroRosterEntry, HeroRosterIcon> onClick, Action<HeroRosterEntry> onDoubleClick = null)
    {
        this.entry = entry;
        this.onClick = onClick;
        this.onDoubleClick = onDoubleClick;

        icon.sprite = entry.Icon;
        tierText.text = entry.Tier.ToString();
        float alpha = entry.State == HeroRosterState.Placed ? 1f : 0f;
        Color c = placedIcon.color;
        c.a = alpha;
        placedIcon.color = c;
        classIcon.sprite = classIcons[entry.Data.HeroType];
        toolTip.SetMessaege(entry.Data.HeroNameKey);
    }

    // 배치된 영웅의 실시간 체력 반영. HeroRosterPanel은 HeroRoster.Changed(배치 상태 변화)로만
    // 아이콘을 다시 그리므로, 전투 중 체력 변화는 여기서 매 프레임 직접 폴링해야 한다.
    private void Update()
    {
        if (hpSlider == null) return;

        if (entry == null || entry.State != HeroRosterState.Placed)
        {
            if (hpSlider.gameObject.activeSelf) hpSlider.gameObject.SetActive(false);
            return;
        }

        // == null로 체크: 합성 시 엔트리를 MarkAvailable() 없이 목록에서만 빼면서 유닛을 파괴하는
        // 기존 버그가 있어 entry.State가 Placed로 남아있는 채로 PlacedUnit이 파괴돼 있을 수 있다.
        GameObject unit = entry.PlacedUnit;
        if (unit == null || !unit.TryGetComponent(out Hero hero))
        {
            if (hpSlider.gameObject.activeSelf) hpSlider.gameObject.SetActive(false);
            return;
        }

        if (!hpSlider.gameObject.activeSelf) hpSlider.gameObject.SetActive(true);
        hpSlider.maxValue = Mathf.Max(1f, hero.MaxHp);
        hpSlider.value = Mathf.Clamp(hero.Hp, 0f, hpSlider.maxValue);
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
