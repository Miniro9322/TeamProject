#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour, IExclusiveUiPanel
{
    [SerializeField] private SettingUI settingPanel;
    [SerializeField] private GameObject QuitAlert;
    [SerializeField] private Button openButton;

    private ClickOutsideCloser outsideCloser;

    private void Awake()
    {
        outsideCloser = new ClickOutsideCloser((RectTransform)transform, openButton != null ? openButton.transform : null);
    }

    // 단축키/버튼/바깥클릭 중 무엇으로 열고 닫히든 SetActive는 결국 여기를 거치므로,
    // ExclusiveUiCoordinator 등록 지점으로 쓴다.
    private void OnEnable()
    {
        ExclusiveUiCoordinator.NotifyOpened(this);
        outsideCloser.MarkOpened();
        settingPanel.gameObject.SetActive(true);
        QuitAlert.SetActive(false);
    }

    private void OnDisable()
    {
        ExclusiveUiCoordinator.NotifyClosed(this);
    }

    public void RequestClose() => OnCloseButton();

    private void Update()
    {
        if (outsideCloser.ShouldClose()) OnCloseButton();
    }

    public void OnCloseButton()
    {
        gameObject.SetActive(false);
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
        SceneManager.LoadScene("TempTitle");
    }
}
