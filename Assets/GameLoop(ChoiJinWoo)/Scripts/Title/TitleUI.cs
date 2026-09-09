using System;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class TitleUI : MonoBehaviour
{
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private GameObject QuitAlert;
    [SerializeField] private GameObject LoadingPanel;
    [SerializeField] private GameObject tutorialChoicePanel; // "튜토리얼 하기" / "건너뛰기" 선택지
    [SerializeField] private Button firstButton; // "튜토리얼 하기" / "건너뛰기" 선택지
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private Button loadButton;
    [SerializeField] private ConfirmPopup confirmPopup;
    private readonly SlotPreviewReader previewReader = new SlotPreviewReader();

    private void OnEnable()
    {
        // Title 씬에는 VContainer 컨테이너가 없어 이 씬의 진입점인 TitleUI가 직접 켠다 - SlotSelectPanel/
        // DaySelectPanel/LoadOptionPopup/UpgradeUI 등 Title 씬의 패널들이 이 신호를 구독한다.
        GlobalUiInputSignals.Enable();
        GlobalUiInputSignals.EscapePerformedFallback += OnEscape;
    }

    private void OnDisable()
    {
        GlobalUiInputSignals.EscapePerformedFallback -= OnEscape;
        GlobalUiInputSignals.Disable();
    }

    // EscapePerformedFallback(최저 우선순위)로 받는다 - upgradePanel처럼 GlobalUiInputSignals를 구독하는
    // 패널이 이번 Esc를 소비하면 이 메서드는 호출되지 않는다. settingPanel/tutorialChoicePanel은 자체
    // Esc 구독이 없어(닫기 버튼/바깥 클릭으로만 닫힘) 여기서 실시간으로 걸러준다 - fallback이 항상
    // 마지막에 돌기 때문에 지난 프레임을 캐싱하지 않아도 값이 어긋나지 않는다.
    // ConfirmPopup(세이브 덮어쓰기 확인창)은 자기 스스로 Esc를 구독하지 않고 Cancel()만 열어두는
    // 방식이라, 여기서 직접 불러줘야 한다.
    private void OnEscape()
    {
        if (confirmPopup != null && confirmPopup.gameObject.activeSelf)
        {
            confirmPopup.Cancel();
            return;
        }

        if (IsAnyPanelOpen()) return;

        OnQuitAlert();
    }

    private bool IsAnyPanelOpen()
    {
        return settingPanel.activeSelf
            || upgradePanel.activeSelf
            || tutorialChoicePanel.activeSelf;
    }

    private void Awake()
    {
        ApplyResolution().Forget();
        ApplyVolume().Forget();
        settingPanel.SetActive(false);
        QuitAlert.SetActive(false);
        upgradePanel.SetActive(false);
        tutorialChoicePanel.SetActive(false);
        RefreshLoad();

        // QuitAlert는 전용 스크립트가 없는 단순 토글 패널이라, 열고 닫는 코드가 어디서 불리든
        // (이 클래스든, 아이콘의 UIButtonHeld.Toggle이든) OnEnable/OnDisable로 ExclusiveUiCoordinator에
        // 등록되도록 여기서 붙여준다 - BuildModePanel.Awake()와 같은 기법.
        if (QuitAlert.GetComponent<ExclusivePanelPresence>() == null)
            QuitAlert.AddComponent<ExclusivePanelPresence>();
    }

    private void Start()
    {
        EnemySoundManager.PlayBgm("TitleBGM");
        EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
    }

    private async UniTaskVoid ApplyResolution()
    {
        await UniTask.Yield();

        int width = PlayerPrefs.GetInt("ResWidth", Screen.currentResolution.width);
        int height = PlayerPrefs.GetInt("ResHeight", Screen.currentResolution.height);
        var mode = (FullScreenMode)PlayerPrefs.GetInt("ScreenMode", (int)FullScreenMode.FullScreenWindow);

        if (Screen.width == width && Screen.height == height && Screen.fullScreenMode == mode)
            return;

        Screen.SetResolution(width, height, mode);
    }

    private async UniTaskVoid ApplyVolume()
    {
        // 오디오 믹서가 초기화된 다음 저장된 값을 적용한다.
        await UniTask.Yield();
        mixer.SetFloat("MasterVolume", AudioVolumeUtil.LinearToDb(PlayerPrefs.GetFloat("MasterVolume", 1f)));
        mixer.SetFloat("BgmVolume", AudioVolumeUtil.LinearToDb(PlayerPrefs.GetFloat("BgmVolume", 1f)));
        mixer.SetFloat("SfxVolume", AudioVolumeUtil.LinearToDb(PlayerPrefs.GetFloat("SfxVolume", 1f)));
        mixer.SetFloat("System", AudioVolumeUtil.LinearToDb(PlayerPrefs.GetFloat("System", 1f)));
    }

    // 저장 여부에 따라 바로 시작하거나 덮어쓰기 확인창을 연다.
    public void OnNewGame()
    {
        if (HasSave())
        {
            ShowOverwrite();
            return;
        }

        BeginNew();
    }

    // 단일 슬롯의 최신 저장 데이터를 선택하고 즉시 진입한다.
    public void OnLoad()
    {
        BeginLoad();
    }

    // 새 게임 덮어쓰기 확인창을 연다.
    private void ShowOverwrite()
    {
        confirmPopup.ShowPopup(BeginNew);
    }

    // 단일 슬롯을 새 게임으로 지정하고 튜토리얼 선택창을 연다.
    private void BeginNew()
    {
        SelectedSaveSlot.SetNewGame(1);
        tutorialChoicePanel.SetActive(true);
    }

    // 단일 슬롯을 불러오기로 지정하고 메인 씬 진입을 시작한다.
    private void BeginLoad()
    {
        SelectedSaveSlot.SetLoad(1);
        EnterMainScene();
    }

    // 튜토리얼 진행 선택을 기록하고 메인 씬으로 이동한다.
    public void OnStartWithTutorial()
    {
        TutorialEntryChoice.Set(skip: false);
        if(tutorialChoicePanel == null) return;
        tutorialChoicePanel.SetActive(false);
        EnterMainScene();
    }

    // 튜토리얼 선택지의 "건너뛰기" 버튼 - 위와 같은 이유로 TutorialEntryChoice에 기록해둔다.
    public void OnSkipTutorial()
    {
        TutorialEntryChoice.Set(skip: true);
        if(tutorialChoicePanel == null) return;
        tutorialChoicePanel.SetActive(false);
        EnterMainScene();
    }

    // 로딩 화면을 띄우고 MainScene으로 넘어간다.
    private void EnterMainScene()
    {
        LoadingPanel.SetActive(true);
        LoadSceneAsync("MainScene").Forget();
    }

    // 저장 데이터 존재 여부에 맞춰 불러오기 버튼을 갱신한다.
    private void RefreshLoad()
    {
        loadButton.gameObject.SetActive(HasSave());
    }

    // 단일 슬롯에 저장 데이터가 있는지 반환한다.
    private bool HasSave()
    {
        return previewReader.HasAnySave();
    }

    private async UniTaskVoid LoadSceneAsync(string sceneName)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(0.2f));

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        while(op.progress < 0.9f)
        {
            await UniTask.Yield();
        }

        await UniTask.WaitForSeconds(1.5f);

        op.allowSceneActivation = true;
        await op;
    }

    public void OnUpgrade()
    {
        if (upgradePanel == null) return;
        SetPanelOpen(upgradePanel, !upgradePanel.activeSelf);
    }

    public void OnSetting()
    {
        SetPanelOpen(settingPanel, !settingPanel.activeSelf);
    }

    public void OnQuitAlert()
    {
        if (QuitAlert.activeSelf)
        {
            SetQuitAlertOpen(false);
            return;
        }

        // 세이브 덮어쓰기 확인창이 떠 있는 동안은 종료 확인창을 그 위에 겹쳐 띄우지 않는다.
        if (confirmPopup != null && confirmPopup.gameObject.activeSelf) return;

        SetQuitAlertOpen(true);
    }

    public void OnCancel()
    {
        SetQuitAlertOpen(false);
    }

    // QuitAlert(Alert 프리팹)에는 PanelReveal이 붙어 있다 - 버튼 클릭이든 Esc든 항상 그 스케일
    // 연출을 거쳐 여닫도록 SetActive 대신 여기를 거친다.
    private void SetQuitAlertOpen(bool open) => SetPanelOpen(QuitAlert, open);

    // settingPanel/upgradePanel도 씬에 PanelReveal이 붙어 있다 - 있으면 그 스케일 연출로,
    // 없으면 예전처럼 SetActive로 여닫는다.
    private static void SetPanelOpen(GameObject panel, bool open)
    {
        PanelReveal reveal = panel.GetComponent<PanelReveal>();
        if (reveal != null)
        {
            if (open) reveal.Show();
            else reveal.Hide();
            return;
        }
        panel.SetActive(open);
    }

    public void OnQuit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif

        Application.Quit();
    }
}
