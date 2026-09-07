using UnityEngine;

public class HeroCreateIconHeldLink : HeldLinkBase
{
    [SerializeField] private GameObject watchedPanel;
    [SerializeField] private HeroSetPanel ownerSource;
    [SerializeField] private HeroCreateIcon ownerIdentity;
    private PanelActivityNotifier notifier;

    protected override void Awake()
    {
        base.Awake();
        notifier = watchedPanel.GetComponent<PanelActivityNotifier>();
        if (notifier == null) notifier = watchedPanel.AddComponent<PanelActivityNotifier>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        notifier.ActiveChanged += OnWatchedActiveChanged;
        ownerSource.OpenIconChanged += Refresh;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        notifier.ActiveChanged -= OnWatchedActiveChanged;
        ownerSource.OpenIconChanged -= Refresh;
    }

    private void OnWatchedActiveChanged(bool active) => Refresh();

    protected override bool CheckHeld()
    {
        return watchedPanel.activeSelf && ownerSource.OpenIcon == ownerIdentity;
    }
}
