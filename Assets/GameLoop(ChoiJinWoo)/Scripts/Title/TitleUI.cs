using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class TitleUI : MonoBehaviour
{
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private AudioMixer mixer;

    private void Awake()
    {
        ApplyResolution().Forget();
        ApplyVolume();
        settingPanel.SetActive(false);
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

    public void OnStart()
    {
        SceneManager.LoadScene("MainScene");
    }

    public void OnUpgrade()
    {

    }

    public void OnSetting()
    {
        settingPanel.SetActive(true);
    }

    public void OnQuit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif

        Application.Quit();
    }
}
