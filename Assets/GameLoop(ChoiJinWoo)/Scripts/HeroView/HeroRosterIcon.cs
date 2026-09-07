using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HeroRosterIcon : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private Button button;
    [SerializeField] private Button retrieveButton;
    [SerializeField] private Image placedIcon;
    [SerializeField] private TextMeshProUGUI tierText;
    [SerializeField] private List<Sprite> classIcons;
    [SerializeField] private Image classIcon;
    [SerializeField] private Slider hpSlider;
    [SerializeField] private HeroStatToolTipTrigger statToolTipTrigger;
    [Tooltip("새로 생성된 영웅에게 보여줄 표시(이미지 등). 비워두면 아무것도 하지 않는다.")]
    [SerializeField] private GameObject newBadge;

    private const float DoubleClickWindow = 0.2f;

    private HeroRosterEntry entry;
    private Action<HeroRosterEntry, HeroRosterIcon> onClick;
    private Action<HeroRosterEntry> onDoubleClick;
    private Action<HeroRosterEntry> onRetrieveClick;
    private CancellationTokenSource pendingSingleClickCts;

    public void PrepareForReuse()
    {
        CancelPendingSingleClick();
        entry = null;
        onClick = null;
        onDoubleClick = null;
        onRetrieveClick = null;
        if (retrieveButton != null) retrieveButton.onClick.RemoveAllListeners();
    }

    private void OnDisable() => CancelPendingSingleClick();

    private void CancelPendingSingleClick()
    {
        pendingSingleClickCts?.Cancel();
        pendingSingleClickCts?.Dispose();
        pendingSingleClickCts = null;
    }

    public void Set(HeroRosterEntry entry, Action<HeroRosterEntry, HeroRosterIcon> onClick,
        Action<HeroRosterEntry> onDoubleClick = null, Action<HeroRosterEntry> onRetrieveClick = null)
    {
        this.entry = entry;
        this.onClick = onClick;
        this.onDoubleClick = onDoubleClick;
        this.onRetrieveClick = onRetrieveClick;

        icon.sprite = entry.Icon;
        tierText.text = entry.Tier.ToString();
        float alpha = entry.State == HeroRosterState.Placed ? 1f : 0f;
        Color c = placedIcon.color;
        c.a = alpha;
        placedIcon.color = c;
        classIcon.sprite = classIcons[entry.Data.HeroType];
        statToolTipTrigger.SetData(entry);

        if (newBadge != null) newBadge.SetActive(entry.IsNew);

        if (retrieveButton != null)
        {
            bool canRetrieve = onRetrieveClick != null && entry.State == HeroRosterState.Placed;
            retrieveButton.gameObject.SetActive(canRetrieve);
            retrieveButton.onClick.RemoveAllListeners();
            if (canRetrieve) retrieveButton.onClick.AddListener(() => onRetrieveClick(entry));
        }
    }

    private void Update()
    {
        if (hpSlider == null) return;

        if (entry == null || entry.State != HeroRosterState.Placed)
        {
            if (hpSlider.gameObject.activeSelf) hpSlider.gameObject.SetActive(false);
            return;
        }

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

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.clickCount >= 2)
        {
            CancelPendingSingleClick();
            onDoubleClick?.Invoke(entry);
            return;
        }

        CancelPendingSingleClick();
        pendingSingleClickCts = new CancellationTokenSource();
        FireSingleClickAfterDelay(pendingSingleClickCts.Token).Forget();
    }

    private async UniTaskVoid FireSingleClickAfterDelay(CancellationToken token)
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(DoubleClickWindow), cancellationToken: token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        pendingSingleClickCts = null;
        onClick?.Invoke(entry, this);
    }
}
