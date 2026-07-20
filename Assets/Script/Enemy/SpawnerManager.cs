using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

// 독립 레인(지역) 스포너들을 한곳에서 관리한다.
// - 지역별 WaveSpawner를 인스펙터로 등록 → 지역번호로 조회
// - 지역 해금 상태(IsUnlockregion) 관리
// - 해금된 지역들에만 웨이브 스폰 지시
// - 모든 해금 지역이 전멸하면 AllRegionsClear 통지
public class SpawnerManager : MonoBehaviour
{
    [Tooltip("각 지역 레인의 WaveSpawner. 순서 무관 — 스포너의 Region 값으로 매핑한다.")]
    [SerializeField] private List<WaveSpawner> spawners = new();

    [Tooltip("게임 시작 시 이미 열려 있는 지역들(예: 시작 지역 1).")]
    [SerializeField] private List<int> startUnlocked = new() { 1 };

    // 지역번호 → 스포너
    private readonly Dictionary<int, WaveSpawner> _byRegion = new();
    // 지역번호 → 해금 여부
    public Dictionary<int, bool> IsUnlockregion = new();

    // 해금된 모든 지역의 적이 전멸했을 때 1회 발생.
    public event Action AllRegionsClear;

    private IObjectResolver _resolver;
    [Inject] public void Construct(IObjectResolver resolver) => _resolver = resolver;

    private static SpawnerManager instance;
    public static SpawnerManager Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("SpawnerManager");
                instance = go.AddComponent<SpawnerManager>();
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        BuildRegistry();
    }

    private void OnDestroy()
    {
        foreach (var s in spawners)
            if (s != null) s.EnemyAllClear -= OnRegionClear;
    }

    // 스포너 목록을 지역번호로 인덱싱하고, 해금 딕셔너리를 초기화한다.
    private void BuildRegistry()
    {
        _byRegion.Clear();
        foreach (var s in spawners)
        {
            if (s == null) continue;
            _byRegion[s.Region] = s;
            if (!IsUnlockregion.ContainsKey(s.Region))
                IsUnlockregion[s.Region] = false; // 기본은 잠금

            s.EnemyAllClear -= OnRegionClear; // 중복 구독 방지
            s.EnemyAllClear += OnRegionClear; // 각 지역 클리어를 매니저가 집계
        }

        foreach (int region in startUnlocked)
            IsUnlockregion[region] = true;
    }

    // ── 해금 ───────────────────────────────────────────────
    public void UnlockRegion(int region)
    {
        IsUnlockregion[region] = true;
    }

    public bool IsUnlocked(int region)
        => IsUnlockregion.TryGetValue(region, out bool v) && v;

    // ── 스폰 ───────────────────────────────────────────────
    // 해금된 모든 지역에 대해 해당 라운드 웨이브를 동시에 스폰한다(밤 시작 시 호출).
    public void SpawnWave(int round)
    {
        foreach (var kv in _byRegion)
        {
            if (!IsUnlocked(kv.Key)) continue;
            kv.Value.SpawnWave(kv.Key, round);
        }
    }

    // 특정 지역만 스폰(테스트/개별 트리거용).
    public void SpawnRegion(int region, int round)
    {
        if (!IsUnlocked(region)) return;
        if (_byRegion.TryGetValue(region, out WaveSpawner s))
            s.SpawnWave(region, round);
    }

    // ── 클리어 집계 ─────────────────────────────────────────
    // 한 지역이 비면 호출됨. 해금된 모든 지역이 비었을 때만 AllRegionsClear 발생.
    // (주의: EnemyBase의 EnemyDieEvent 호출이 살아있어야 각 스포너 Enemycount가 줄어든다.)
    private void OnRegionClear()
    {
        foreach (var kv in _byRegion)
            if (IsUnlocked(kv.Key) && kv.Value.Enemycount > 0)
                return; // 아직 남은 지역이 있음

        AllRegionsClear?.Invoke();
    }
}
