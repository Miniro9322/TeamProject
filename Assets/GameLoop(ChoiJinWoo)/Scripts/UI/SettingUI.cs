using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingUI : MonoBehaviour, IExclusiveUiPanel
{
    [SerializeField] private TMP_Dropdown screenMode;
    [SerializeField] private AudioMixer gameAudioMixer;
    [SerializeField] private Slider masterVolume;
    [SerializeField] private Slider bgmVolume;
    [SerializeField] private Slider sfxVolume;
    [SerializeField] private Slider systemVolume;
    private readonly FullScreenMode[] screenModes =
    {
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

    [SerializeField] private RectTransform openButton;
    private ClickOutsideCloser outsideCloser;
    private PanelReveal panelReveal;

    private void Awake()
    {
        outsideCloser = new ClickOutsideCloser((RectTransform)transform, openButton);
        panelReveal = GetComponent<PanelReveal>();
    }

    private void OnEnable()
    {
        outsideCloser.MarkOpened();
        if (Closed == null) ExclusiveUiCoordinator.NotifyOpened(this);
        GlobalUiInputSignals.ClickPerformed += HandleCloseCheck;
        // 독립 실행(Title 씬)일 때만 Esc로 직접 닫는다. Main 씬에선 Closed 구독자(MenuUI)가 닫기를
        // 주관하므로 HandleCloseCheck가 Closed != null로 조기 반환한다. 예전엔 TitleUI의 별도
        // escapeAction과 처리 순서가 안 맞아 Esc 한 번에 설정창이 닫히면서 종료창까지 같이 떠서
        // 이 구독을 빼 뒀지만, 이제 Esc가 우선순위 체인(EscapePerformed에서 소비 → TitleUI의
        // fallback은 호출 안 됨)으로 처리되므로 그 충돌이 없다.
        GlobalUiInputSignals.EscapePerformed += HandleCloseCheck;

        screenMode.ClearOptions();

        var options = new List<string>
        {
            DataTableManager.StringTable.Get("UI_Setting_BorderlessWindow"),
            DataTableManager.StringTable.Get("UI_Setting_Window"),
        };
        screenMode.AddOptions(options);

        int savedMode = PlayerPrefs.GetInt("ScreenMode", (int)FullScreenMode.FullScreenWindow);
        int modeIndex = Array.IndexOf(screenModes, (FullScreenMode)savedMode);
        screenMode.value = modeIndex >= 0 ? modeIndex : 0;
        screenMode.RefreshShownValue();

        screenMode.onValueChanged.AddListener(ChangeScreenMode);

        LocalizeTextManager.OnLanguageChanged += RefreshDropdown;

        foreach (var (slider, param) in VolumeSliders)
        {
            slider.value = PlayerPrefs.GetFloat(param, 1f);
            ApplyVolume(param, slider.value);
            slider.onValueChanged.AddListener(v => SetVolume(param, v));
        }

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
            if (resolutions[i].width == Screen.width &&
                resolutions[i].height == Screen.height)
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
        if (Closed == null) ExclusiveUiCoordinator.NotifyClosed(this);
        GlobalUiInputSignals.ClickPerformed -= HandleCloseCheck;
        GlobalUiInputSignals.EscapePerformed -= HandleCloseCheck;

        foreach (var (slider, _) in VolumeSliders)
        {
            slider.onValueChanged.RemoveAllListeners();
        }
        screenMode.onValueChanged.RemoveAllListeners();
        screenWide.onValueChanged.RemoveAllListeners();
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

    public event System.Action Closed;

    public void RequestClose() => OnClose();

    public void OnClose()
    {
        if (Closed == null)
        {
            if (panelReveal != null) panelReveal.Hide();
            else gameObject.SetActive(false);
        }
        Closed?.Invoke();
    }

    private void HandleCloseCheck()
    {
        if (Closed != null) return;
        if (outsideCloser.ShouldClose())
        {
            OnClose();
        }
    }

    private void ChangeScreenMode(int index)
    {
        var mode = screenModes[index];
        if (Screen.fullScreenMode == mode) return;

        Screen.fullScreenMode = mode;
        PlayerPrefs.SetInt("ScreenMode", (int)mode);
    }

    private void SetVolume(string paramName, float sliderValue)
    {
        ApplyVolume(paramName, sliderValue);
        PlayerPrefs.SetFloat(paramName, sliderValue);
    }

    private void ApplyVolume(string paramName, float sliderValue)
    {
        gameAudioMixer.SetFloat(paramName, AudioVolumeUtil.LinearToDb(sliderValue));
    }

    private void RefreshDropdown()
    {
        screenMode.ClearOptions();

        var options = new List<string>
        {
            DataTableManager.StringTable.Get("UI_Setting_FullScreen"),
            DataTableManager.StringTable.Get("UI_Setting_BorderlessWindow"),
            DataTableManager.StringTable.Get("UI_Setting_Window"),
        };
        screenMode.AddOptions(options);
    }
}
