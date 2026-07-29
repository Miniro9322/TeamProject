using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SettingUI : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown screenMode;
    [SerializeField] private AudioMixer gameAudioMixer;
    [SerializeField] private Slider masterVolume;
    [SerializeField] private Slider bgmVolume;
    [SerializeField] private Slider sfxVolume;
    [SerializeField] private Slider systemVolume;
    private readonly FullScreenMode[] screenModes =
    {
        FullScreenMode.ExclusiveFullScreen,
        FullScreenMode.FullScreenWindow,
        FullScreenMode.Windowed,
    };
    private Resolution[] resolutions;
    [SerializeField] private TMP_Dropdown screenWide;
    private static readonly (int width, int height)[] commonResolutions =
    {
        (1280, 720),
        (1600, 900),
        (1920, 1080),
        (2560, 1440),
        (3840, 2160),
    };

    private void OnEnable()
    {
        //창 모드
        screenMode.ClearOptions();

        var options = new List<string>
        {
            "전체 화면",
            "테두리 없는 창",
            "창 모드",
        };
        screenMode.AddOptions(options);

        screenMode.value = PlayerPrefs.GetInt("ScreenMode");
        screenMode.RefreshShownValue();

        screenMode.onValueChanged.AddListener(ChangeScreenMode);

        //오디오
        masterVolume.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
        bgmVolume.value = PlayerPrefs.GetFloat("BgmVolume", 1f);
        sfxVolume.value = PlayerPrefs.GetFloat("SfxVolume", 1f);
        systemVolume.value = PlayerPrefs.GetFloat("System", 1f);

        ApplyVolume("MasterVolume", masterVolume.value);
        ApplyVolume("BgmVolume", bgmVolume.value);
        ApplyVolume("SfxVolume", sfxVolume.value);
        ApplyVolume("System", systemVolume.value);

        masterVolume.onValueChanged.AddListener(v => SetVolume("MasterVolume", v));
        bgmVolume.onValueChanged.AddListener(v => SetVolume("BgmVolume", v));
        sfxVolume.onValueChanged.AddListener(v => SetVolume("SfxVolume", v));
        systemVolume.onValueChanged.AddListener(v => SetVolume("System", v));

        //해상도
        resolutions = Screen.resolutions
            .GroupBy(r => (r.width, r.height))
            .Select(g => g.First())
            .Where(r => commonResolutions.Contains((r.width, r.height)))
            .OrderBy(r => r.width)
            .ToArray();

        if (resolutions.Length == 0)
            resolutions = new[] { Screen.currentResolution };

        screenWide.ClearOptions();

        var screenoptions = new List<string>();
        int currentIndex = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            screenoptions.Add($"{resolutions[i].width} x {resolutions[i].height}");
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentIndex = i;
            }
        }

        screenWide.AddOptions(screenoptions);
        screenWide.value = currentIndex;
        screenWide.RefreshShownValue();

        screenWide.onValueChanged.AddListener(OnResolutionChanged);
    }

    private void OnDisable()
    {
        masterVolume.onValueChanged.RemoveAllListeners();
        bgmVolume.onValueChanged.RemoveAllListeners();
        sfxVolume.onValueChanged.RemoveAllListeners();
        systemVolume.onValueChanged.RemoveAllListeners();
        screenMode.onValueChanged.RemoveAllListeners();
    }

    private void OnResolutionChanged(int index)
    {
        var res = resolutions[index];

        if (Screen.width == res.width && Screen.height == res.height)
            return;

        Screen.SetResolution(res.width, res.height, Screen.fullScreenMode);
        PlayerPrefs.SetInt("ResWidth", res.width);
        PlayerPrefs.SetInt("ResHeight", res.height);
    }

    public void OnClose()
    {
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if(Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            gameObject.SetActive(false);
        }
    }

    private void ChangeScreenMode(int index)
    {
        var mode = screenModes[index];
        if (Screen.fullScreenMode == mode) return;

        Screen.fullScreenMode = screenModes[index];
        PlayerPrefs.SetInt("ScreenMode", index);
    }

    private void SetVolume(string paramName, float sliderValue)
    {
        ApplyVolume(paramName, sliderValue);
        PlayerPrefs.SetFloat(paramName, sliderValue);
    }

    private void ApplyVolume(string paramName, float sliderValue)
    {
        float dB = sliderValue > 0.0001f ? Mathf.Log10(sliderValue) * 20f : -80f;
        gameAudioMixer.SetFloat(paramName, dB);
    }
}
