using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

// 독립 레인(지역) 스포너들을 한곳에서 관리한다.
// - 지역별 WaveSpawner를 인스펙터로 등록 → 지역번호로 조회
// - 지역 해금 상태(IsUnlockregion) 관리
// - 해금된 지역들에만 웨이브 스폰 지시
// - 모든 해금 지역이 전멸하면 AllRegionsClear 통지
public class SpawnerManager : MonoBehaviour
{
    [SerializeField] private List<WaveSpawner> spawners = new();

    [SerializeField] private List<int> startUnlocked = new() { 1 };

    // 지역번호 → 스포너
    private readonly Dictionary<int, WaveSpawner> _byRegion = new();
    // 지역번호 → 해금 여부
    public Dictionary<int, bool> IsUnlockregion = new();
    public ModuleLogic[] moduleLogics;
    // 해금된 모든 지역의 적이 전멸했을 때 1회 발생.
    public Camera cam;
    public event Action AllRegionsClear;
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
        foreach(var m in moduleLogics)
        {
            if(m==null)continue;
            m.OnStateChanged += state =>
                UnlockRegion(m.ModuleId,m.CurrentState);
        }
    }

    private void OnDestroy()
    {
        foreach (var s in spawners)
            if (s != null) s.EnemyAllClear -= OnRegionClear;
    }
    void Update()
    {
        InputClick();
    }
    private void InputClick()
    {
        if (cam == null) cam = Camera.main;          // 인스펙터 미할당 시 메인 카메라로 폴백
        if (cam == null || Mouse.current == null) return;
        if(Time.timeScale==0)return;
        if(!Mouse.current.leftButton.wasPressedThisFrame) return;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        foreach (var kv in _byRegion)
        {
            WaveSpawner spawner = kv.Value;
            if (spawner == null || spawner.Board == null) continue;

            Tile tile = spawner.Board.CellFromRay(ray);
            if (tile == null || !tile.isEnemySpawn) continue; // 이 지역 보드의 소환지점 타일이 아니면 skip
            if(!IsUnlocked(kv.Key))continue;
            spawner.OnClickStage(kv.Key, CurrentDay);          // 그 지역의 현재 날짜 웨이브 정보 표시
            return;                                            // 맞는 지역 하나 찾으면 끝
        }
    }
    private IObjectResolver _resolver;
    [Inject] public void Construct(IObjectResolver resolver) => _resolver = resolver;
    private GameManager _gameManager;
    private int CurrentDay
    {
        get
        {
            if (_gameManager == null && _resolver != null) _gameManager = _resolver.Resolve<GameManager>();
            return _gameManager != null ? _gameManager.DayCount : 0;
        }
    }
    private void BuildRegistry()
    {
        _byRegion.Clear();
        foreach (var s in spawners)
        {
            if (s == null) continue;
            _byRegion[s.Region] = s;
            if (!IsUnlockregion.ContainsKey(s.Region))
                IsUnlockregion[s.Region] = false;

            s.EnemyAllClear -= OnRegionClear; 
            s.EnemyAllClear += OnRegionClear; 
        }

        foreach (int region in startUnlocked)
            IsUnlockregion[region] = true;
    }

    public void UnlockRegion(int region,ModuleState state)
    {
        if(state!=ModuleState.Locked)
        IsUnlockregion[region] = true; //해금 할때 씀
        else
        IsUnlockregion[region] = false;
    }

    public bool IsUnlocked(int region)
        => IsUnlockregion.TryGetValue(region, out bool v) && v; //해금 확인용

    public void SpawnWave(int round) //해당라운드 전체소환
    {
        foreach (var kv in _byRegion)
        {
            if (!IsUnlocked(kv.Key)) continue;

            kv.Value.SpawnWave(kv.Key,round);
        }
    }


    public void SpawnRegion(int region,int round) //해당 지역 라운드 소환(테스트 용)
    {
        if (!IsUnlocked(region)) return;
        if (_byRegion.TryGetValue(region, out WaveSpawner s))
            s.SpawnWave(region, round);
    }
    private void OnRegionClear() //몹 다잡았을때
    {
        foreach (var kv in _byRegion)
            if (IsUnlocked(kv.Key) && kv.Value.Enemycount > 0)
                return; // 아직 남은 지역이 있음

        AllRegionsClear?.Invoke();
    }
}
