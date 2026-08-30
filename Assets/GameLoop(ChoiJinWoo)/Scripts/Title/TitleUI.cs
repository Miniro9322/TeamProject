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
    [SerializeField] private ConfirmPopup confirmPopup;
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
