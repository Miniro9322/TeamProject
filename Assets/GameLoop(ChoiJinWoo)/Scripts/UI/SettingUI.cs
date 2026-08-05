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

    private (Slider slider, string param)[] VolumeSliders => new[]
    {
        (masterVolume, "MasterVolume"),
        (bgmVolume, "BgmVolume"),
        (sfxVolume, "SfxVolume"),
        (systemVolume, "System"),
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

        int savedMode = PlayerPrefs.GetInt("ScreenMode", (int)FullScreenMode.FullScreenWindow);
        int modeIndex = Array.IndexOf(screenModes, (FullScreenMode)savedMode);
        screenMode.value = modeIndex >= 0 ? modeIndex : 0;
        screenMode.RefreshShownValue();

        screenMode.onValueChanged.AddListener(ChangeScreenMode);

        //오디오
        foreach (var (slider, param) in VolumeSliders)
        {
            slider.value = PlayerPrefs.GetFloat(param, 1f);
            ApplyVolume(param, slider.value);
            slider.onValueChanged.AddListener(v => SetVolume(param, v));
        }

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
        foreach (var (slider, _) in VolumeSliders)
        {
            slider.onValueChanged.RemoveAllListeners();
        }
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

        Screen.fullScreenMode = mode;
        PlayerPrefs.SetInt("ScreenMode", (int)mode); // 인덱스 대신 실제 enum 값 저장
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
