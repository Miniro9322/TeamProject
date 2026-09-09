using System;
using System.Runtime.ConstrainedExecution;
using Cysharp.Threading.Tasks;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using VContainer;

public class UiManager : MonoBehaviour
{
    [SerializeField] private RequestSupportUi requestSupportUi;
    [SerializeField] private GameObject gamaOverUi;
    [SerializeField] private GameObject gameSpeedUi;
    private GameSpeedUI gameSpeedUiComponent;
    private RectTransform gameSpeedUiRect;
    public GameSpeedUI GameSpeedUi => gameSpeedUiComponent;
    public RectTransform GameSpeedUiRect => gameSpeedUiRect;
    [SerializeField] private GameObject menuPanel;
    private PanelReveal menuPanelReveal;
    [SerializeField] private GameObject guidePanel;
    private PanelReveal guidePanelReveal;
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI upgradeResourceText;
    [SerializeField] private Key guideOpenKey = Key.G;

    public byte UnlockedHero;
    public byte UnlockedEnemy;

    public event Action UnlockChanged;
    private InputAction guideOpenAction;

    private UiPanelStack panelStack;
    private BuildModePanel buildModePanel;
    private AddCitizen addCitizen;

    [Inject]
    private void Construct(UiPanelStack panelStack, BuildModePanel buildModePanel, AddCitizen addCitizen)
    {
        this.panelStack = panelStack;
        this.buildModePanel = buildModePanel;
        this.addCitizen = addCitizen;
    }

    private void Awake()
    {
        gameSpeedUiComponent = gameSpeedUi.GetComponent<GameSpeedUI>();
        gameSpeedUiRect = gameSpeedUi.GetComponent<RectTransform>();
        menuPanelReveal = menuPanel.GetComponent<PanelReveal>();
        guidePanelReveal = guidePanel.GetComponent<PanelReveal>();

        requestSupportUi.gameObject.SetActive(false);
        gamaOverUi.SetActive(false);
        gameSpeedUi.SetActive(false);
        menuPanel.SetActive(false);
        guidePanel.SetActive(false);
        requestSupportUi.OnUnlock += UpdateUnlock;

        GlobalUiInputSignals.Enable();
        GlobalUiInputSignals.EscapePerformedFallback += HandleEscape;

        guideOpenAction = new InputAction("OpenGuide", binding: Keyboard.current[guideOpenKey].path);
        guideOpenAction.performed += OnGuideOpenPerformed;
        guideOpenAction.Enable();
    }

    private void OnDestroy()
    {
        GlobalUiInputSignals.EscapePerformedFallback -= HandleEscape;
        GlobalUiInputSignals.Disable();

        guideOpenAction.performed -= OnGuideOpenPerformed;
        guideOpenAction.Disable();
        guideOpenAction.Dispose();
    }

    // 이제 EscapePerformedFallback(가장 낮은 우선순위)로 받는다 - GlobalUiInputSignals를 구독하는
    // 패널이 이번 Esc를 소비했으면 이 메서드는 호출되지 않는다. 그래서 예전처럼 지난 프레임 상태를
    // LateUpdate로 캐싱할 필요가 없다. HasEscapeCloseTarget()가 남아 있는 건 도감(EnemyArchiveManager)
    // 처럼 GlobalUiInputSignals를 안 거치고 자체적으로 Esc를 처리하는 패널까지 걸러주기 위함이고,
    // fallback이 항상 마지막에 돌기 때문에 실시간 조회로도 값이 어긋나지 않는다.
    private void HandleEscape()
    {
        if (menuPanel.activeSelf)
        {
            CloseMenuPanel();
            return;
        }

        if (TutorialInputGate.BlockPanelOpen) return;
        if (HasEscapeCloseTarget()) return;

        ShowMenuPanel();
    }

    private void OnGuideOpenPerformed(InputAction.CallbackContext context)
    {
        if (!TutorialInputGate.BlockHotkeys) OpenGuide();
    }

    // GlobalUiInputSignals를 거치지 않는 별도 Esc 경로를 가진 패널(도감 등)이 열려 있으면 ESC로 메뉴를 열지 않는다.
    private bool HasEscapeCloseTarget()
    {
        return (panelStack != null && panelStack.HasAny)
            || guidePanel.activeInHierarchy
            || (addCitizen != null && addCitizen.gameObject.activeInHierarchy)
            || (buildModePanel != null && buildModePanel.HasEscapeCancelable)
            || (EnemyArchiveManager.Instance != null && EnemyArchiveManager.Instance.IsOpen)
            || SpawnerManager.IsStageInfoOpen;
    }

    public void ToggleGameSpeedUi(bool value)
    {
        gameSpeedUi.SetActive(value);
    }

    public async UniTask OpenRequestSupportUi()
    {
        requestSupportUi.gameObject.SetActive(true);
        await UniTask.WaitUntil(() => requestSupportUi.gameObject.activeSelf == false);
    }

    private void UpdateUnlock(byte unlock)
    {
        UnlockedHero |= unlock;
        UnlockChanged?.Invoke();
    }

    public void OpenGameOverUI(int daycount, int point)
    {
        dayText.text = string.Format(DataTableManager.StringTable.Get("Ui_GameOverSurviveDay"), daycount);
        upgradeResourceText.text = $"{point}";
        gamaOverUi.SetActive(true);
    }

    public void OnTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Title");
    }

    public void OnQuit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif

        Application.Quit();
    }

    public void OpenMenuPanel()
    {
        if (TutorialInputGate.BlockPanelOpen) return;

        if (menuPanel.activeSelf == false)
            ShowMenuPanel();
        else
            CloseMenuPanel();
    }

    private void ShowMenuPanel()
    {
        if (menuPanelReveal == null) { menuPanel.SetActive(true); return; }
        menuPanelReveal.Show();
    }

    private void CloseMenuPanel()
    {
        if (menuPanelReveal == null) { menuPanel.SetActive(false); return; }
        menuPanelReveal.Hide();
    }

    public void OpenGuide()
    {
        if (TutorialInputGate.BlockPanelOpen) return;

        if (guidePanelReveal == null)
        {
            guidePanel.SetActive(!guidePanel.activeSelf);
            return;
        }

        if (guidePanel.activeSelf == false)
            guidePanelReveal.Show();
        else
            guidePanelReveal.Hide();
    }
}
