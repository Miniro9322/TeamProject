using System;
using System.Collections.Generic;
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
    }

    private void OnDisable()
    {
        masterVolume.onValueChanged.RemoveAllListeners();
        bgmVolume.onValueChanged.RemoveAllListeners();
        sfxVolume.onValueChanged.RemoveAllListeners();
        systemVolume.onValueChanged.RemoveAllListeners();
        screenMode.onValueChanged.RemoveAllListeners();
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
