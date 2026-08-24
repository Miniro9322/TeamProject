using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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


    private void Awake()
    {
        ApplyResolution().Forget();
        ApplyVolume();
        settingPanel.SetActive(false);
        QuitAlert.SetActive(false);
        upgradePanel.SetActive(false);
        tutorialChoicePanel.SetActive(false);
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

    // "시작" 버튼 - 바로 씬으로 넘어가지 않고 튜토리얼 여부부터 물어본다. 이 선택은 게임 씬의
    // DI 컨테이너가 뜨기 전에(GameManager.Construct()가 TutorialState.Seen을 동기적으로 읽기 전에)
    // 확정돼야 해서, 씬 전환 전인 여기서 정한다.
    public void OnStart()
    {
        tutorialChoicePanel.SetActive(true);
    }

    // 튜토리얼 선택지의 "튜토리얼 하기" 버튼 - 명시적으로 다시 보겠다는 요청이니, 예전에 이미
    // 끝까지 봐서 TutorialSeen이 true로 남아있더라도 여기서 강제로 초기화해 반드시 뜨게 한다.
    public void OnStartWithTutorial()
    {
        new TutorialState().Reset();
        tutorialChoicePanel.SetActive(false);
        StartGame();
    }

    // 튜토리얼 선택지의 "건너뛰기" 버튼 - TutorialState는 PlayerPrefs만 다루는 plain class라
    // DI 없이 바로 만들어 써도 된다.
    public void OnSkipTutorial()
    {
        new TutorialState().MarkSeen();
        tutorialChoicePanel.SetActive(false);
        StartGame();
    }

    private void StartGame()
    {
        LoadingPanel.SetActive(true);
        LoadSceneAsync("MainScene").Forget();
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
        upgradePanel.SetActive(true);
    }

    public void OnSetting()
    {
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
