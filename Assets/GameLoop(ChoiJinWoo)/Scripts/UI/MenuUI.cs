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

    // 나가기 직전 저장을 맡길 저장 관리자를 받아 둔다
    [Inject]
    private void Construct(SaveManager saveManager)
    {
        this.saveManager = saveManager;
    }

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
        PanelPopIn.Play((RectTransform)settingPanel.transform);
        QuitAlert.SetActive(false);

        // 행/컬럼 크기를 매번 재계산하던 중첩 ContentSizeFitter는 크기를 고정값으로 박고 제거했다
        // (ContentSizeFitterFreezer.cs 참고) - 이제 SetActive 직후 남은 LayoutGroup들이 고정된
        // 크기 안에서 자식 위치만 정렬하면 되므로, 한 패스만 강제로 즉시 확정해도 충분하다.
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)settingPanel.transform);
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
        saveManager.SaveDayActive();
        Time.timeScale = 1f;
        SceneManager.LoadScene("Title");
    }
}
