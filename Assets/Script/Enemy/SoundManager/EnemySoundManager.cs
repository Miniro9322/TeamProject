using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class EnemySoundManager : MonoBehaviour
{
    public static EnemySoundManager Instance { get; private set; }
    [SerializeField] private EnemySoundDataBase db;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource bgmSource;
    
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [SerializeField] private AudioMixer mixer;
    [SerializeField] private string masterParam = "MasterVolume";
    [SerializeField] private string sfxParam = "SfxVolume";
    [SerializeField] private string bgmParam = "BgmVolume";


    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 1f;


    private const string PrefMaster = "vol_master";
    private const string PrefSfx = "vol_sfx";
    private const string PrefBgm = "vol_bgm";

    // 같은 키 효과음이 너무 짧은 간격으로 중복 재생되는 것만 막는 스로틀.
    // 상태를 SoundDatabase(SO 에셋)에 저장하면 에디터 세션 간에 값이 남아 소리가 안 나므로,
    // 런타임 전용 딕셔너리에 보관한다(세션마다 초기화).
    private const float PlayThrottle = 0.05f;
    private readonly Dictionary<string, float> lastPlayTime = new Dictionary<string, float>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (sfxSource != null) sfxSource.playOnAwake = false;
        if (bgmSource != null) { bgmSource.playOnAwake = false; bgmSource.loop = true; }

        // 저장된 볼륨 복원 (없으면 인스펙터 기본값 사용)
        masterVolume = PlayerPrefs.GetFloat(PrefMaster, masterVolume);
        sfxVolume = PlayerPrefs.GetFloat(PrefSfx, sfxVolume);
        bgmVolume = PlayerPrefs.GetFloat(PrefBgm, bgmVolume);
        ApplyMixer();
    }
    void Start()
    {
        if (masterSlider != null) masterSlider.value = masterVolume;
        if (sfxSlider != null)    sfxSlider.value = sfxVolume;
        if (bgmSlider != null)    bgmSlider.value = bgmVolume;
    }

    private void ApplyMixer()
    {
        if (mixer == null) return;
        mixer.SetFloat(masterParam, LinearToDb(masterVolume));
        mixer.SetFloat(sfxParam, LinearToDb(sfxVolume));
        mixer.SetFloat(bgmParam, LinearToDb(bgmVolume));
    }

    // 슬라이더 초기값 표시용 (설정창 열 때 호출)
    public float GetMasterVolume() => masterVolume;
    public float GetSfxVolume() => sfxVolume;
    public float GetBgmVolume() => bgmVolume;

    // 선형 0~1 → 데시벨 변환 (믹서는 dB로 동작)
    private static float LinearToDb(float v)
    {
        if (v <= 0.0001f) return -80f;
        return Mathf.Log10(v) * 20f;
    }

    // 효과음 재생 (중복 재생 허용)
    public static void Play(string key)
    {
        if (Instance == null || Instance.db == null) return;
        var e = Instance.db.Get(key);
        if (e == null) { Debug.LogWarning($"SoundDatabase에 '{key}' 키 없음"); return; }
        if (Instance.sfxSource == null) { Debug.LogWarning("sfxSource 미할당"); return; }

        // 같은 키 중복 재생 스로틀: 런타임 딕셔너리 + 언스케일드 타임(일시정지 timeScale=0 영향 없음)
        float now = Time.unscaledTime;
        if (Instance.lastPlayTime.TryGetValue(key, out float last) && now - last < PlayThrottle) return;
        Instance.lastPlayTime[key] = now;

        // 카테고리(SFX) 볼륨은 믹서가 담당. 여기선 클립별 상대 볼륨만 적용
        Instance.sfxSource.PlayOneShot(e.clip, e.volume);
    }

    // BGM 재생 (같은 곡이면 무시, 다르면 교체)
    public static void PlayBgm(string key)
    {
        if (Instance == null || Instance.db == null) return;
        var e = Instance.db.Get(key);
        if (e == null) { Debug.LogWarning($"SoundDatabase에 '{key}' 키 없음"); return; }
        if (Instance.bgmSource == null) { Debug.LogWarning("bgmSource 미할당"); return; }

        var src = Instance.bgmSource;
        if (src.isPlaying && src.clip == e.clip) return;

        // 카테고리(BGM) 볼륨은 믹서가 담당. 여기선 클립별 상대 볼륨만 적용
        src.clip = e.clip;
        src.volume = e.volume;
        src.loop = e.loop;
        src.Play();
    }

    public static void StopBgm()
    {
        if (Instance == null || Instance.bgmSource == null) return;
        Instance.bgmSource.Stop();
    }
    public void SetMasterVolume(float v)
    {
        masterVolume = Mathf.Clamp01(v);
        if (mixer != null) mixer.SetFloat(masterParam, LinearToDb(masterVolume));
        PlayerPrefs.SetFloat(PrefMaster, masterVolume);
    }

    public void SetSfxVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);
        if (mixer != null) mixer.SetFloat(sfxParam, LinearToDb(sfxVolume));
        PlayerPrefs.SetFloat(PrefSfx, sfxVolume);
    }

    public void SetBgmVolume(float v)
    {
        bgmVolume = Mathf.Clamp01(v);
        if (mixer != null) mixer.SetFloat(bgmParam, LinearToDb(bgmVolume));
        PlayerPrefs.SetFloat(PrefBgm, bgmVolume);
    }
}
