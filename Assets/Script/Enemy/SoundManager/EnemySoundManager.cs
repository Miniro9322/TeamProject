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

    // soundTime이 들어있는 클립 전용 재생 슬롯.
    // 공용 sfxSource의 PlayOneShot으로는 중간에 못 끊는다 — 끊으려면 sfxSource.Stop()인데
    // 그러면 그 순간 재생 중인 다른 효과음까지 전부 죽는다. 그래서 이 클립들만
    // 각자 AudioSource를 하나씩 물고 재생하고, 시간이 되면 그 소스만 멈춘다.
    private class TimedVoice
    {
        public AudioSource source;
        public float endTime;   // Time.unscaledTime 기준
        public bool manualStop; // true인 동안은 Update()의 자동 종료 스캔에서 제외 — 외부가 직접 Stop()할 때까지 유지되는 루프 전용
    }
    private readonly List<TimedVoice> timedVoices = new List<TimedVoice>();

    // BGM 크로스페이드 상태. 코루틴이 아니라 Update로 도는 이유는 위 timedVoices와 같다
    // (씬 전환/오브젝트 비활성화 시에도 안전하게 끊기도록).
    private enum BgmFadeState { None, FadeOut, FadeIn }
    private BgmFadeState fadeState = BgmFadeState.None;
    private float fadeTimer;
    private float fadeDuration;
    private float fadeStartVolume;
    private EnemySoundDataBase.Entry pendingBgmEntry;

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
    // soundTime이 0 이하면 클립을 끝까지 재생한다(기존 동작 그대로).
    // soundTime이 들어있으면 그 초가 지날 때 잘라낸다 — 긴 클립을 연출 길이에 맞춰 쓸 때.
    // ignoreThrottle: 한 발의 공격이 짧은 시간 안에 같은 키를 여러 번 의도적으로 재생해야 하는 경우
    // (예: 라인 관통 화살이 적을 연속으로 맞힐 때) 아래 스로틀을 우회한다 — 스로틀은 서로 무관한
    // 소스가 우연히 겹치는 걸 막기 위한 것이라, 한 번의 공격 안에서 일부러 반복 재생하는 경우까지
    // 막으면 안 된다.
    public static void Play(string key, bool ignoreThrottle = false)
    {
        if (Instance == null || Instance.db == null) return;
        var e = Instance.db.Get(key);
        if (e == null) { Debug.LogWarning($"SoundDatabase에 '{key}' 키 없음"); return; }

        // 같은 키 중복 재생 스로틀: 런타임 딕셔너리 + 언스케일드 타임(일시정지 timeScale=0 영향 없음)
        float now = Time.unscaledTime;
        if (!ignoreThrottle)
        {
            if (Instance.lastPlayTime.TryGetValue(key, out float last) && now - last < PlayThrottle) return;
            Instance.lastPlayTime[key] = now;
        }

        if (e.soundTime > 0f) { Instance.PlayTimed(e, now); return; }

        if (Instance.sfxSource == null) { Debug.LogWarning("sfxSource 미할당"); return; }
        // 카테고리(SFX) 볼륨은 믹서가 담당. 여기선 클립별 상대 볼륨만 적용
        Instance.sfxSource.PlayOneShot(e.clip, e.volume);
    }

    // 명시적으로 멈출 때까지 유지되는 루프 사운드. PlayThrottle(중복 재생 스로틀)은 적용하지
    // 않는다 — 같은 키를 쓰는 여러 인스턴스(예: 장판 여러 개)가 동시에 각자 독립적으로 재생/정지
    // 되어야 하기 때문. 반환된 AudioSource를 호출부가 들고 있다가 필요할 때 직접 Stop()해야 한다.
    public static AudioSource PlayLoop(string key)
    {
        if (Instance == null || Instance.db == null) return null;
        var e = Instance.db.Get(key);
        if (e == null) { Debug.LogWarning($"SoundDatabase에 '{key}' 키 없음"); return null; }

        TimedVoice voice = Instance.GetFreeVoice();
        voice.manualStop = true;
        AudioSource src = voice.source;
        src.clip = e.clip;
        src.volume = e.volume;
        src.loop = true; // DB entry의 loop 값과 무관하게 강제 루프
        src.Play();
        return src;
    }

    // soundTime이 있는 클립을 전용 소스로 재생하고 마감 시각을 예약한다.
    // 마감 시각을 unscaledTime으로 잡는 게 핵심 — 이 기능을 처음 쓰는 곳이 보스 연출인데
    // 거기는 Time.timeScale=0으로 얼려놓고 돌아간다(SpawnerManager.BossOpeningDirecting).
    // 스케일드 시간으로 재면 얼어있는 동안 시계가 안 흘러 영영 안 끊긴다.
    private void PlayTimed(EnemySoundDataBase.Entry e, float now)
    {
        TimedVoice voice = GetFreeVoice();
        voice.manualStop = false; // 이 슬롯이 과거에 PlayLoop로 쓰였던 경우에도 정상적으로 자동 종료되도록 리셋
        AudioSource src = voice.source;
        src.clip = e.clip;
        src.volume = e.volume;
        src.loop = e.loop;      // 루프 + soundTime = "이 초 동안만 반복"
        src.Play();
        voice.endTime = now + e.soundTime;
    }

    // 놀고 있는 슬롯을 재사용하고, 없으면 하나 더 만든다.
    // 새로 만든 소스도 반드시 SFX 믹서 그룹에 물려야 설정창 볼륨이 먹는다(RouteToMixer와 같은 이유).
    private TimedVoice GetFreeVoice()
    {
        for (int i = 0; i < timedVoices.Count; i++)
        {
            TimedVoice v = timedVoices[i];
            if (v.source != null && !v.source.isPlaying) return v;
        }

        AudioSource src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.outputAudioMixerGroup = ResolveGroup(sfxGroup, "SFX");
        // 인스펙터에서 잡아둔 sfxSource와 같은 톤으로 들리게 맞춘다.
        // 특히 spatialBlend — 새 AudioSource는 기본이 2D라, sfxSource가 3D면 이것만 소리가 다르게 난다.
        if (sfxSource != null)
        {
            src.spatialBlend = sfxSource.spatialBlend;
            src.rolloffMode = sfxSource.rolloffMode;
            src.minDistance = sfxSource.minDistance;
            src.maxDistance = sfxSource.maxDistance;
            src.priority = sfxSource.priority;
            src.bypassEffects = sfxSource.bypassEffects;
            src.bypassListenerEffects = sfxSource.bypassListenerEffects;
        }

        var created = new TimedVoice { source = src };
        timedVoices.Add(created);
        return created;
    }

    // 예약된 마감 시각이 지난 슬롯만 멈춘다. 멈추면 isPlaying이 꺼져 자동으로 재사용 대상이 된다.
    // 코루틴 대신 Update로 도는 이유: 오브젝트가 꺼지거나 씬이 바뀔 때 코루틴만 죽고 소리가 남는 걸 피한다.
    private void Update()
    {
        if (timedVoices.Count > 0)
        {
            float now = Time.unscaledTime;
            for (int i = 0; i < timedVoices.Count; i++)
            {
                TimedVoice v = timedVoices[i];
                if (v.source == null || !v.source.isPlaying) continue;
                if (v.manualStop) continue;
                if (now < v.endTime) continue;
                v.source.Stop();
            }
        }

        UpdateBgmFade();
    }

    // BGM 재생 (같은 곡이면 무시, 다르면 교체).
    // fadeDuration이 0이면 기존과 동일하게 즉시 교체(보스 등장 등 임팩트가 필요한 전환용).
    // fadeDuration > 0이면 현재 곡을 그만큼 페이드 아웃한 뒤 새 곡을 페이드 인한다.
    public static void PlayBgm(string key, float fadeDuration = 0f)
    {
        if (Instance == null || Instance.db == null) return;
        var e = Instance.db.Get(key);
        if (e == null) { Debug.LogWarning($"SoundDatabase에 '{key}' 키 없음"); return; }
        if (Instance.bgmSource == null) { Debug.LogWarning("bgmSource 미할당"); return; }

        var src = Instance.bgmSource;
        if (src.isPlaying && src.clip == e.clip) return;

        if (fadeDuration <= 0f)
        {
            Instance.fadeState = BgmFadeState.None; // 진행 중이던 페이드가 있으면 취소하고 즉시 전환
            src.clip = e.clip;
            src.volume = e.volume;
            src.loop = e.loop;
            src.Play();
            return;
        }

        Instance.StartBgmFade(e, fadeDuration);
    }

    private void StartBgmFade(EnemySoundDataBase.Entry entry, float duration)
    {
        pendingBgmEntry = entry;
        fadeDuration = duration;
        fadeTimer = 0f;

        if (bgmSource.isPlaying)
        {
            fadeStartVolume = bgmSource.volume;
            fadeState = BgmFadeState.FadeOut;
        }
        else
        {
            // 최초 재생: 페이드 아웃할 대상이 없으니 바로 새 곡을 볼륨 0으로 깔고 페이드 인만 한다.
            bgmSource.clip = entry.clip;
            bgmSource.loop = entry.loop;
            bgmSource.volume = 0f;
            bgmSource.Play();
            fadeState = BgmFadeState.FadeIn;
        }
    }

    private void UpdateBgmFade()
    {
        if (fadeState == BgmFadeState.None) return;

        fadeTimer += Time.unscaledDeltaTime;
        float t = fadeDuration > 0f ? Mathf.Clamp01(fadeTimer / fadeDuration) : 1f;

        if (fadeState == BgmFadeState.FadeOut)
        {
            bgmSource.volume = Mathf.Lerp(fadeStartVolume, 0f, t);
            if (t >= 1f)
            {
                bgmSource.clip = pendingBgmEntry.clip;
                bgmSource.loop = pendingBgmEntry.loop;
                bgmSource.Play();
                fadeState = BgmFadeState.FadeIn;
                fadeTimer = 0f;
            }
        }
        else // FadeIn
        {
            bgmSource.volume = Mathf.Lerp(0f, pendingBgmEntry.volume, t);
            if (t >= 1f)
            {
                bgmSource.volume = pendingBgmEntry.volume;
                fadeState = BgmFadeState.None;
            }
        }
    }

    public static void StopBgm()
    {
        if (Instance == null || Instance.bgmSource == null) return;
        Instance.fadeState = BgmFadeState.None;
        Instance.bgmSource.Stop();
    }

    // 후보 키 중 하나를 무작위로 골라 PlayBgm으로 재생한다. 낮/밤처럼 곡을 여러 개 돌려쓰고 싶을 때 사용.
    public static void PlayRandomBgm(string[] keys, float fadeDuration = 0f)
    {
        if (keys == null || keys.Length == 0) return;
        PlayBgm(keys[UnityEngine.Random.Range(0, keys.Length)], fadeDuration);
    }
}
