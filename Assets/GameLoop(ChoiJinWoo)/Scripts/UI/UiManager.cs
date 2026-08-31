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
    // 튜토리얼이 pauseTimeWhileActive 단계에서 Time.timeScale을 직접 안 건드리고 이 컴포넌트의
    // API(OnButtonClick)로 배속을 걸기 위해 참조한다 - gameSpeedUi는 UiManager 프리팹의 자식이라
    // 런타임에야 생성되므로 다른 컴포넌트가 인스펙터로 직접 드래그해 연결할 수 없다.
    public GameSpeedUI GameSpeedUi => gameSpeedUiComponent;
    // 같은 이유로 배속 패널의 RectTransform도 인스펙터 드래그가 불가능하다 - TutorialManager가
    // GameSpeedMention 스텝의 waypoint target을 런타임에 이 값으로 채워 넣는 데 쓴다.
    public RectTransform GameSpeedUiRect => gameSpeedUiRect;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject guidePanel;
    private PanelReveal guidePanelReveal;
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI upgradeResourceText;
    [SerializeField] private Key MenuKey = Key.Escape;
    [SerializeField] private Key guideOpenKey = Key.G;

    public byte UnlockedHero;
    public byte UnlockedEnemy;

    public event Action UnlockChanged;
    private Keyboard keyboard;

    private UiPanelStack panelStack;
    private BuildModePanel buildModePanel;
    private AddCitizen addCitizen;
    // ESC가 눌린 "이전 프레임 끝" 시점에 닫을 게 있었는지를 담아둔다 - 같은 프레임 안에서 다른
    // 패널들의 Update()가 이 스크립트보다 먼저/나중에 도는지는 보장이 안 되므로, 그 프레임 자체의
    // 상태를 실시간으로 물어보면 이미 닫힌 뒤라 "닫을 게 없었다"고 오판할 수 있다. 그래서 한 프레임 전
    // (LateUpdate에서 찍어둔) 스냅샷으로 판단해 Update 실행 순서와 무관하게 만든다.
    private bool hadEscapeCloseTargetLastFrame;

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
        guidePanelReveal = guidePanel.GetComponent<PanelReveal>();

        requestSupportUi.gameObject.SetActive(false);
        gamaOverUi.SetActive(false);
        gameSpeedUi.SetActive(false);
        menuPanel.SetActive(false);
        guidePanel.SetActive(false);
        requestSupportUi.OnUnlock += UpdateUnlock;
        keyboard = Keyboard.current;
    }

    private void Update()
    {
        if (keyboard == null) return;

        if (keyboard[MenuKey].wasPressedThisFrame && !TutorialInputGate.BlockEscapeClose)
        {
            if (menuPanel.activeSelf)
                menuPanel.SetActive(false);
            else if (!hadEscapeCloseTargetLastFrame) // ESC로 닫거나 취소할 다른 게 있으면 그것부터 - 메뉴는 안 연다
                menuPanel.SetActive(true);
        }

        if (keyboard[guideOpenKey].wasPressedThisFrame && !TutorialInputGate.BlockHotkeys)
            OpenGuide();
    }

    private void LateUpdate()
    {
        hadEscapeCloseTargetLastFrame = HasEscapeCloseTarget();
    }

    // 이 패널들이 각자 자기 Update()에서 ESC를 보고 스스로 닫는 것과 별개로, 메뉴를 열지 말지
    // 판단하는 데만 쓰는 상태 조회다 - 실제로 닫는 동작은 여전히 각 패널이 담당한다.
    private bool HasEscapeCloseTarget()
    {
        return (panelStack != null && panelStack.HasAny)
            || guidePanel.activeInHierarchy
            || (addCitizen != null && addCitizen.gameObject.activeInHierarchy)
            || (buildModePanel != null && buildModePanel.HasEscapeCancelable)
            || (EnemyArchiveManager.Instance != null && EnemyArchiveManager.Instance.IsOpen);
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
        // 튜토리얼이 영웅 배치 대기 중일 땐 이 패널이 맵을 덮어 배치를 끝낼 수 없게 된다 - TutorialInputGate.cs 참고.
        if (TutorialInputGate.BlockPanelOpen) return;

        if(menuPanel.activeSelf == false)
            menuPanel.SetActive(true);
        else
            menuPanel.SetActive(false);
    }

    public void OpenGuide()
    {
        // 튜토리얼이 영웅 배치 대기 중일 땐 이 패널이 맵을 덮어 배치를 끝낼 수 없게 된다 - TutorialInputGate.cs 참고.
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
