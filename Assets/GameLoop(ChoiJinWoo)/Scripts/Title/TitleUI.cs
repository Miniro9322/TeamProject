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
    [SerializeField] private AudioMixer mixer;

    [SerializeField] private Button loadButton;
    [SerializeField] private SlotSelectPanel slotSelectPanel;
 
    private readonly SlotPreviewReader previewReader = new SlotPreviewReader();


    private void Awake()
    {
        ApplyResolution().Forget();
        ApplyVolume();
        settingPanel.SetActive(false);
        QuitAlert.SetActive(false);
        upgradePanel.SetActive(false);
        slotSelectPanel.gameObject.SetActive(false);
        slotSelectPanel.SlotConfirmed += OnSlotConfirmed;
        slotSelectPanel.SaveChanged += RefreshLoad;
        slotSelectPanel.SetPreviewReader(previewReader);
        RefreshLoad();
    }

    // 슬롯 패널 이벤트 구독을 해제한다.
    private void OnDestroy()
    {
        slotSelectPanel.SlotConfirmed -= OnSlotConfirmed;
        slotSelectPanel.SaveChanged -= RefreshLoad;
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


    // "새 게임 시작" 버튼: 슬롯 선택 패널을 새 게임 모드로 연다.
    public void OnNewGame()
    {
        slotSelectPanel.OpenForNewGame();
    }
   // "불러오기" 버튼: 슬롯 선택 패널을 불러오기 모드로 연다.
    public void OnLoad()
    {
        slotSelectPanel.OpenForLoad();
    }

    // SlotSelectPanel.SlotConfirmed 구독자: 슬롯이 확정되면 MainScene으로 넘어간다.
    private void OnSlotConfirmed()
    {
        EnterMainScene();
    }

    // 로딩 화면을 띄우고 MainScene으로 넘어가는 본체(OnSlotConfirmed에서 재사용).
    private void EnterMainScene()
    {
        LoadingPanel.SetActive(true);
        LoadSceneAsync("MainScene").Forget();
    }

    // 저장 데이터 존재 여부에 맞춰 불러오기 버튼을 갱신한다.
    private void RefreshLoad()
    {
        loadButton.interactable = previewReader.HasAnySave();
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
