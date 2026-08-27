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

    [Header("중요 사운드(Warning 등) — 다른 소리에 묻히지 않게 한다")]
    [Tooltip("PlayImportant로 낸 소리를 SFX가 아니라 System 그룹으로 내보낸다 — 효과음 무리와 채널이 분리되고 SFX 슬라이더에도 안 깎인다.")]
    [SerializeField] private bool importantUsesSystemGroup = true;
    [Tooltip("중요 사운드를 낼 때 이미 울리고 있던 효과음 잔향을 끊는다. 보스 연출은 timeScale=0이라 새 효과음은 안 나고 직전 프레임 잔향만 남아 덮는다.")]
    [SerializeField] private bool importantCutsSfxTails = true;
    [Tooltip("중요 사운드가 나는 동안 BGM을 이 배율로 줄인다(덕킹). 1=안 줄임, 0=무음.")]
    [Range(0f, 1f)][SerializeField] private float importantBgmDuck = 0.25f;
    [Tooltip("눌리는 데 걸리는 시간(초). 짧을수록 딱 끊기듯 들어간다.")]
    [SerializeField] private float duckFadeIn = 0.08f;
    [Tooltip("원래 볼륨으로 돌아오는 시간(초). 길수록 자연스럽다.")]
    [SerializeField] private float duckRelease = 0.5f;

    // 매번 FindMatchingGroups를 돌지 않도록 캐시. 보이스를 재사용할 때 그룹을 다시 지정해야 해서 자주 쓴다.
    private AudioMixerGroup sfxGroupCached, systemGroupCached;
    private AudioMixerGroup SfxGroup => sfxGroupCached != null ? sfxGroupCached : (sfxGroupCached = ResolveGroup(sfxGroup, "SFX"));
    private AudioMixerGroup SystemGroup => systemGroupCached != null ? systemGroupCached : (systemGroupCached = ResolveGroup(systemGroup, "System"));

    // 덕킹 상태. 믹서의 노출 파라미터(SfxVolume/BgmVolume)는 SettingUI 소유라 절대 건드리지 않는다 —
    // 대신 AudioSource.volume(소스별 배율)만 곱한다. 이건 믹서 볼륨과 독립이라 유저 설정을 덮어쓰지 않는다.
    private float bgmBaseVolume = 1f;   // PlayBgm이 넣어준 원래 볼륨. 덕킹 해제 시 여기로 돌아온다.
    private float duckUntil;            // Time.unscaledTime 기준. 이 시각까지 눌러 둔다.
    private float duckLevel;            // 0=원래 볼륨 / 1=완전히 눌림

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

        if (e.soundTime > 0f) { Instance.PlayTimed(e, now, Instance.SfxGroup); return; }

        if (Instance.sfxSource == null) { Debug.LogWarning("sfxSource 미할당"); return; }
        // 카테고리(SFX) 볼륨은 믹서가 담당. 여기선 클립별 상대 볼륨만 적용
        Instance.sfxSource.PlayOneShot(e.clip, e.volume);
    }

    /// <summary>
    /// 반드시 들려야 하는 소리(보스 Warning 등). Play와 달리 세 가지를 같이 한다:
    ///   ① System 그룹으로 내보내 효과음 무리와 채널을 분리하고,
    ///   ② 이미 울리고 있던 효과음 잔향을 끊고,
    ///   ③ 재생하는 동안 BGM을 눌러(덕킹) 자리를 비운다.
    /// AudioSource.priority로는 해결되지 않는다 — 그건 보이스 한계를 넘길 때만 쓰이는 값이고,
    /// 효과음은 전부 sfxSource 하나로 나가서 우선순위가 애초에 동일하다.
    /// </summary>
    public static void PlayImportant(string key)
    {
        if (Instance == null || Instance.db == null) return;
        var e = Instance.db.Get(key);
        if (e == null) { Debug.LogWarning($"SoundDatabase에 '{key}' 키 없음"); return; }
        Instance.PlayImportantInternal(e);
    }

    private void PlayImportantInternal(EnemySoundDataBase.Entry e)
    {
        // sfxSource.Stop()은 그 소스에서 울리던 원샷을 전부 죽인다 — 평소엔 단점이지만 여기선 목적 그대로다.
        // timedVoices는 각자 다른 AudioSource라 여기 안 걸린다(진행 중인 PlayLoop는 계속 울린다).
        if (importantCutsSfxTails && sfxSource != null) sfxSource.Stop();

        float now = Time.unscaledTime;
        // soundTime으로 잘리는 클립이면 그 길이, 아니면 클립 전체 길이만큼 눌러 둔다.
        float length = e.soundTime > 0f ? e.soundTime : (e.clip != null ? e.clip.length : 0f);
        duckUntil = Mathf.Max(duckUntil, now + length);   // 연달아 불려도 더 늦은 쪽을 남긴다

        AudioMixerGroup group = importantUsesSystemGroup ? SystemGroup : SfxGroup;
        if (e.soundTime > 0f) { PlayTimed(e, now, group); return; }

        // soundTime이 없으면 중간에 끊을 일이 없으니 전용 슬롯이 필요 없다 — 그룹만 맞춰 원샷.
        AudioSource src = importantUsesSystemGroup && systemSource != null ? systemSource : sfxSource;
        if (src == null) { Debug.LogWarning("중요 사운드를 낼 AudioSource가 없음(systemSource/sfxSource 미할당)"); return; }
        src.PlayOneShot(e.clip, e.volume);
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
        // 슬롯은 재사용된다 — 직전에 PlayImportant가 쓴 슬롯이면 System 그룹에 물려 있으므로 매번 되돌린다.
        src.outputAudioMixerGroup = Instance.SfxGroup;
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
    private void PlayTimed(EnemySoundDataBase.Entry e, float now, AudioMixerGroup group)
    {
        TimedVoice voice = GetFreeVoice();
        voice.manualStop = false; // 이 슬롯이 과거에 PlayLoop로 쓰였던 경우에도 정상적으로 자동 종료되도록 리셋
        AudioSource src = voice.source;
        // 슬롯 재사용 때문에 그룹은 매번 명시한다 — 안 하면 직전 재생의 그룹이 그대로 남는다.
        src.outputAudioMixerGroup = group;
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
        // 아래 timedVoices 가드보다 반드시 위 — 타임드 보이스가 하나도 없을 때도 덕킹은 풀려야 한다.
        TickDuck();

        if (timedVoices.Count == 0) return;

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

    // 덕킹을 목표치로 서서히 밀어붙인다. 시간 소스는 unscaled — 보스 연출은 timeScale=0으로 얼려놓고 돈다.
    private void TickDuck()
    {
        float target = Time.unscaledTime < duckUntil ? 1f : 0f;
        if (duckLevel == target) return;   // 변화 없으면 매 프레임 volume을 다시 쓰지 않는다

        float span = Mathf.Max(0.01f, target > duckLevel ? duckFadeIn : duckRelease);
        duckLevel = Mathf.MoveTowards(duckLevel, target, Time.unscaledDeltaTime / span);
        ApplyBgmDuck();
    }

    // 믹서의 BgmVolume(설정창 소유)이 아니라 소스별 배율만 곱한다 — 유저 설정과 서로 간섭하지 않는다.
    private void ApplyBgmDuck()
    {
        if (bgmSource == null) return;
        bgmSource.volume = bgmBaseVolume * Mathf.Lerp(1f, importantBgmDuck, duckLevel);
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
        // 원래 볼륨은 따로 기억한다 — 덕킹이 이 값을 기준으로 곱했다 되돌린다.
        // src.volume에 직접 넣으면 덕킹 중에 곡이 바뀌었을 때 눌린 값이 "원래 볼륨"으로 굳어버린다.
        Instance.bgmBaseVolume = e.volume;
        Instance.ApplyBgmDuck();
        src.loop = e.loop;
        src.Play();
    }

    public static void StopBgm()
    {
        if (Instance == null || Instance.bgmSource == null) return;
        Instance.bgmSource.Stop();
    }
}
