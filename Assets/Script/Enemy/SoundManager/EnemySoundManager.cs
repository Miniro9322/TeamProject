using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class EnemySoundManager : MonoBehaviour
{
    public static EnemySoundManager Instance { get; private set; }
    [SerializeField] private EnemySoundDataBase db;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource systemSource;

    // 볼륨 값은 SettingUI(설정창)가 믹서에 직접 쓴다. 여기선 값을 읽기만 하고 절대 쓰지 않는다.
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private string masterParam = "MasterVolume";
    [SerializeField] private string sfxParam = "SfxVolume";
    [SerializeField] private string bgmParam = "BgmVolume";
    [SerializeField] private string systemParam = "System";

    // 각 소스를 어느 믹서 그룹으로 내보낼지. 비워두면 아래 이름으로 믹서에서 찾아 붙인다.
    [SerializeField] private AudioMixerGroup sfxGroup;
    [SerializeField] private AudioMixerGroup bgmGroup;
    [SerializeField] private AudioMixerGroup systemGroup;

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

        RouteToMixer();
    }

    // 소스를 믹서 그룹에 연결한다. 연결돼 있어야 설정창에서 바꾼 믹서 볼륨이 실제 출력에 반영된다.
    private void RouteToMixer()
    {
        if (sfxSource != null) sfxSource.outputAudioMixerGroup = ResolveGroup(sfxGroup, "SFX");
        if (bgmSource != null) bgmSource.outputAudioMixerGroup = ResolveGroup(bgmGroup, "BGM");
        if (systemSource != null) systemSource.outputAudioMixerGroup = ResolveGroup(systemGroup, "System");
    }

    private AudioMixerGroup ResolveGroup(AudioMixerGroup assigned, string groupName)
    {
        if (assigned != null) return assigned;
        if (mixer == null) return null;

        // FindMatchingGroups는 경로 부분 일치라 이름이 정확히 같은 그룹을 우선 고른다
        var groups = mixer.FindMatchingGroups(groupName);
        foreach (var g in groups)
            if (g.name == groupName) return g;
        return groups.Length > 0 ? groups[0] : null;
    }

    // 현재 믹서에 설정된 볼륨(0~1) 조회용. 출력 감쇠는 믹서가 하므로 재생 코드에서 곱하지 않는다.
    public float GetMasterVolume() => GetMixerVolume(masterParam);
    public float GetSfxVolume() => GetMixerVolume(sfxParam);
    public float GetBgmVolume() => GetMixerVolume(bgmParam);
    public float GetSystemVolume() => GetMixerVolume(systemParam);

    private float GetMixerVolume(string param)
    {
        if (mixer == null || !mixer.GetFloat(param, out float db)) return 1f;
        return DbToLinear(db);
    }

    // 데시벨 → 선형 0~1 변환 (믹서는 dB로 동작)
    private static float DbToLinear(float db)
    {
        if (db <= -80f) return 0f;
        return Mathf.Pow(10f, db / 20f);
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
}
