using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

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
    [SerializeField] private SlotSelectPanel slotSelectPanel;
    //[SerializeField] private DaySelectPanel daySelectPanel;

    private readonly SlotPreviewReader previewReader = new SlotPreviewReader();

    private InputAction escapeAction;

    private void OnEnable()
    {
        escapeAction = new InputAction("Escape", binding: "<Keyboard>/escape");
        escapeAction.performed += OnEscapePerformed;
        escapeAction.Enable();
    }

    private void OnDisable()
    {
        escapeAction.performed -= OnEscapePerformed;
        escapeAction.Disable();
        escapeAction.Dispose();
    }

    // 열려 있는 패널이 없을 때만 종료 확인창을 띄운다 - 패널이 열려 있으면 각 패널이 알아서 Esc를 처리한다.
    private void OnEscapePerformed(InputAction.CallbackContext context)
    {
        if (IsAnyPanelOpen()) return;

        OnQuitAlert();
    }

    private bool IsAnyPanelOpen()
    {
        return settingPanel.activeSelf
            || upgradePanel.activeSelf
            || tutorialChoicePanel.activeSelf
            || slotSelectPanel.gameObject.activeSelf;
    }

    private void Awake()
    {
        ApplyResolution().Forget();
        ApplyVolume().Forget();
        settingPanel.SetActive(false);
        QuitAlert.SetActive(false);
        upgradePanel.SetActive(false);
        tutorialChoicePanel.SetActive(false);
        slotSelectPanel.gameObject.SetActive(false);
        //daySelectPanel.gameObject.SetActive(false);
        slotSelectPanel.SlotConfirmed += OnSlotConfirmed;
        slotSelectPanel.SaveChanged += RefreshLoad;
        slotSelectPanel.SetPreviewReader(previewReader);
        //daySelectPanel.SlotConfirmed += OnSlotConfirmed;
        RefreshLoad();
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
        // ApplyResolution과 같은 이유 - Awake 시점엔 오디오 믹서가 아직 초기화 중이라
        // 여기서 바로 SetFloat을 부르면 값이 씹힌다. 한 프레임 양보한 뒤에 적용한다.
        await UniTask.Yield();

        // PlayerPrefs엔 선형(0~1) 슬라이더 값이 저장돼 있다 - SettingUI와 동일하게 dB로 변환해서 넣어야 한다.
        mixer.SetFloat("MasterVolume", AudioVolumeUtil.LinearToDb(PlayerPrefs.GetFloat("MasterVolume", 1f)));
        mixer.SetFloat("BgmVolume", AudioVolumeUtil.LinearToDb(PlayerPrefs.GetFloat("BgmVolume", 1f)));
        mixer.SetFloat("SfxVolume", AudioVolumeUtil.LinearToDb(PlayerPrefs.GetFloat("SfxVolume", 1f)));
        mixer.SetFloat("System", AudioVolumeUtil.LinearToDb(PlayerPrefs.GetFloat("System", 1f)));
    }

    // "시작" 버튼과 "새 게임" 버튼 모두 여기로 온다 - 슬롯을 먼저 고르게 한다.
    public void OnStart() => OnNewGame();

    public void OnNewGame()
    {
        if (slotSelectPanel == null) return;

        if(slotSelectPanel.gameObject.activeSelf && slotSelectPanel.Mode == SlotSelectMode.NewGame)
            slotSelectPanel.gameObject.SetActive(false);
        else
            slotSelectPanel.OpenForNewGame();
    }

    // "불러오기" 버튼: 슬롯 선택 패널을 불러오기 모드로 연다.
    public void OnLoad()
    {
        if(slotSelectPanel == null) return;

        if (slotSelectPanel.gameObject.activeSelf && slotSelectPanel.Mode == SlotSelectMode.Load)
            slotSelectPanel.gameObject.SetActive(false);
        else
            slotSelectPanel.OpenForLoad();
    }

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

    private void OnSlotConfirmed(SlotSelectMode mode)
    {
        slotSelectPanel.gameObject.SetActive(false);

        if (mode == SlotSelectMode.NewGame)
        {
            tutorialChoicePanel.SetActive(true);
            return;
        }

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
        loadButton.gameObject.SetActive(previewReader.HasAnySave());
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

        if(upgradePanel.activeSelf)
            upgradePanel.SetActive(false);
        else
            upgradePanel.SetActive(true);
    }

    public void OnSetting()
    {
        if(settingPanel.activeSelf)
            settingPanel.SetActive(false);
        else
            settingPanel.SetActive(true);
    }

    public void OnQuitAlert()
    {
        if(QuitAlert.activeSelf)
            QuitAlert.SetActive(false);
        else
            QuitAlert.SetActive(true);
    }

    public void OnCancel()
    {
        QuitAlert.SetActive(false);
    }

    public void OnQuit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif

        Application.Quit();
    }
}
