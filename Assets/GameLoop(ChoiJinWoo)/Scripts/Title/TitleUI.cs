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

public class TitleUI : MonoBehaviour
{
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private GameObject QuitAlert;
    [SerializeField] private GameObject LoadingPanel;
    [SerializeField] private GameObject tutorialChoicePanel; // "튜토리얼 하기" / "건너뛰기" 선택지
    [SerializeField] private AudioMixer mixer;

    [SerializeField] private Button loadButton;
    [SerializeField] private SlotSelectPanel slotSelectPanel;
    //[SerializeField] private DaySelectPanel daySelectPanel;

    private readonly SlotPreviewReader previewReader = new SlotPreviewReader();


    private void Awake()
    {
        ApplyResolution().Forget();
        ApplyVolume();
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

    private void ApplyVolume()
    {
        mixer.SetFloat("MasterVolume", PlayerPrefs.GetFloat("MasterVolume", 0f));
        mixer.SetFloat("BgmVolume", PlayerPrefs.GetFloat("BgmVolume", 0f));
        mixer.SetFloat("SfxVolume", PlayerPrefs.GetFloat("SfxVolume", 0f));
        mixer.SetFloat("System", PlayerPrefs.GetFloat("System", 0f));
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

    // 튜토리얼 선택지의 "튜토리얼 하기" 버튼 - 명시적으로 보겠다는 요청이다. TutorialState는 이제
    // 세이브 슬롯별로 저장되는데, 새로 고른 슬롯엔 아직 파일이 없거나(또는 그 슬롯을 쓰던 이전
    // 세이브의 오래된 파일만 있어) 여기선 파일을 직접 건드리는 대신 이번 진입에 한해 씬 전환 동안만
    // 값을 들고 가는 TutorialEntryChoice에 기록해둔다 - MainScene의 TutorialState가 이를 최우선으로 읽는다.
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

    // 슬롯 선택 확인 직후 - 새 게임일 때만 씬 전환 전에 튜토리얼 선택 화면을 끼워 넣는다.
    // GameManager.Construct()가 TutorialState.Seen을 동기적으로 읽기 전에(씬 전환 전에) 확정돼야
    // 해서 여기서 정한다. 불러오기는 이미 진행 중인 세이브라 물어볼 필요 없이 곧장 넘어간다
    // (TutorialState가 그 세이브의 파일에서 직접 읽는다).
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
