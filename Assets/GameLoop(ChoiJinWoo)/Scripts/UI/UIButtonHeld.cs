using UnityEngine;
using UnityEngine.UI;

public class UIButtonHeld : HeldLinkBase
{
    [SerializeField] private GameObject watchedPanel;

    public GameObject WatchedPanel => watchedPanel;

    private Button button;
    private PanelReveal panelReveal;
    private PanelActivityNotifier notifier;

    protected override void Awake()
    {
        base.Awake();
        button = GetComponent<Button>();
        panelReveal = watchedPanel.GetComponent<PanelReveal>();
        if (button != null) button.onClick.AddListener(Toggle);

        notifier = watchedPanel.GetComponent<PanelActivityNotifier>();
        if (notifier == null) notifier = watchedPanel.AddComponent<PanelActivityNotifier>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        notifier.ActiveChanged += OnWatchedActiveChanged;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        notifier.ActiveChanged -= OnWatchedActiveChanged;
    }

    private void OnWatchedActiveChanged(bool active) => Refresh();

    protected override bool CheckHeld()
    {
        return watchedPanel.activeSelf;
    }

    public void Toggle()
    {
        bool isOpen = watchedPanel.activeSelf;

        if (panelReveal != null)
        {
            if (isOpen) panelReveal.Hide();
            else panelReveal.Show();
        }
        else
        {
            watchedPanel.SetActive(!isOpen);
        }
    }
}
