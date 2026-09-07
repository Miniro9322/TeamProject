#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;

public class MenuUI : MonoBehaviour, IExclusiveUiPanel
{
    [SerializeField] private SettingUI settingPanel;
    [SerializeField] private GameObject QuitAlert;
    [SerializeField] private Button openButton;

    private ClickOutsideCloser outsideCloser;
    private SaveManager saveManager;
    private PanelReveal panelReveal;

    [Inject]
    private void Construct(SaveManager saveManager)
    {
        this.saveManager = saveManager;
    }

    private void Awake()
    {
        outsideCloser = new ClickOutsideCloser((RectTransform)transform, openButton != null ? openButton.transform : null);
        panelReveal = GetComponent<PanelReveal>();
    }

    private void OnEnable()
    {
        ExclusiveUiCoordinator.NotifyOpened(this);
        outsideCloser.MarkOpened();
        transform.SetAsLastSibling();
        settingPanel.Closed += OnCloseButton;
        settingPanel.gameObject.SetActive(true);
        PanelPopIn.Play((RectTransform)settingPanel.transform);
        QuitAlert.SetActive(false);
        GlobalUiInputSignals.ClickPerformed += HandleCloseCheck;
        GlobalUiInputSignals.EscapePerformed += HandleCloseCheck;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)settingPanel.transform);
    }

    private void OnDisable()
    {
        ExclusiveUiCoordinator.NotifyClosed(this);
        GlobalUiInputSignals.ClickPerformed -= HandleCloseCheck;
        GlobalUiInputSignals.EscapePerformed -= HandleCloseCheck;
        settingPanel.Closed -= OnCloseButton;
    }

    public void RequestClose() => OnCloseButton();

    private void HandleCloseCheck()
    {
        if (outsideCloser.ShouldClose()) OnCloseButton();
    }

    public void OnCloseButton()
    {
        if (panelReveal != null) panelReveal.Hide();
        else gameObject.SetActive(false);
    }

    public void OnQuitButton()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif

        Application.Quit();
    }

    public void OnQuitAlert()
    {
        QuitAlert.SetActive(true);
    }

    public void OnCancelQuit()
    {
        if (QuitAlert.activeSelf)
            QuitAlert.SetActive(false);
    }

    public void OnTitle()
    {
        saveManager.SaveDayActive();
        Time.timeScale = 1f;
        SceneManager.LoadScene("Title");
    }
}
