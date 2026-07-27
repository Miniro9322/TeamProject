using System;
using System.Collections.Generic;
using TMPro;
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
    public GameObject spawnPoint;
    private float yOffset = 1f;
    // 지역번호 → 그 지역에 이번 라운드 활성화된 포탈들(레인마다 1개).
    public Dictionary<int,List<GameObject>> spawnPoints = new();

    [Tooltip("구역 클릭 시 포탈 옆에 뜨는 월드 스페이스 텍스트 프리팹(TMP_Text 포함).")]
    public GameObject infoTextPrefab;
    [Tooltip("포탈 기준 텍스트 오프셋(월드 좌표).")]
    public Vector3 infoTextOffset = new Vector3(1.5f, 1f, 0f);
    // 지역번호 → 클릭 시 띄운 정보 텍스트 인스턴스
    private readonly Dictionary<int,GameObject> _infoTexts = new();
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
    private bool changeCheck;

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
        changeCheck = true;
    }

    private void OnDestroy()
    {
        foreach (var s in spawners)
            if (s != null) s.EnemyAllClear -= OnRegionClear;
        
        if(_gameManager!=null)
        {
            _gameManager.ChangeToDay-=ChangeDay;
            _gameManager.ChangeToNight-=ChangeNight;
        }
    }


    void Update()
    {
        if(changeCheck)
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
            ShowStageInfo(kv.Key); // 포탈 옆에 정보 텍스트 띄우고 그 지역의 웨이브 정보 채우기
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
    void Start()
    {
        if (_gameManager == null && _resolver != null)
        _gameManager = _resolver.Resolve<GameManager>();
        if(_gameManager!=null)
        {
            _gameManager.ChangeToDay+=ChangeDay;
            _gameManager.ChangeToNight+=ChangeNight;
        }
        ShowAllPortals();
    }
    private void ChangeDay()
    {
        changeCheck =true;
        HideAllPortals();
        ShowAllPortals();
    }
    private void ChangeNight()
    {
        changeCheck = false;
        HideStageInfos(); // 밤엔 정보 텍스트 제거
    }

    // 해금된 모든 지역에 포탈 표시(낮). 이미 떠 있으면 중복 생성하지 않는다.
    private void ShowAllPortals()
    {
        if (spawnPoint == null) return;
        foreach (var kv in _byRegion)
            if (IsUnlocked(kv.Key)) ShowPortal(kv.Key);
    }

    // 한 지역에만 포탈 표시. 확장(해금) 시에도 이 함수로 즉시 생성한다.
    // 이번 라운드에 켤 레인을 랜덤으로 뽑아, 활성 레인 시작점마다 포탈을 하나씩 세운다.
    private void ShowPortal(int region)
    {
        if (spawnPoint == null) return;
        if (spawnPoints.TryGetValue(region, out var existing) && existing != null && existing.Count > 0) return; // 이미 존재

        if (!_byRegion.TryGetValue(region, out var spawner) || spawner == null || spawner.Board == null) return;
        
        // 보스 라운드(10의 배수 일차)엔 1지역만 코어에서 가장 먼 "끝 구석" 포탈 1개로 고정.
        // 그 외엔 포탈 테이블 Count만큼 랜덤 활성화(밤 스폰과 같은 집합 공유).
        if (region == 1 && CurrentDay > 0 && CurrentDay % 10 == 0)
            spawner.ActivateCornerPortal();
        else
            spawner.RollActivePortals(PortalCount(region));
        var paths = spawner.ActivePaths;
        if (paths == null || paths.Count == 0) return;

        var portals = new List<GameObject>(paths.Count);
        Vector3 lift = Vector3.up * yOffset;
        foreach (var path in paths)
        {
            if (path == null || path.Count == 0) continue;
            portals.Add(PoolManager.Instance.Spawn(spawnPoint, path[0] + lift, Quaternion.identity));
        }
        spawnPoints[region] = portals;
    }

    // 포탈 테이블(Region,Id,Count)에서 이번 지역·라운드에 열 포탈 수를 읽는다.
    // Id는 WaveTable과 같은 라운드 체계(10일차 초과는 1001~1005 순환). 행이 없으면 1개.
    private int PortalCount(int region)
    {
        PortalTable table = DataTableManager.Get<PortalTable>(DataTableIds.Portal);
        if (table == null) return 1;
        int id = WaveSpawner.GetStageLookupId(CurrentDay);
        return table.GetCount(region, id, 1);
    }

    private void HideAllPortals()
    {
        if (spawnPoint == null) return;
        foreach (var kv in spawnPoints)
        {
            if (kv.Value == null) continue;
            foreach (var portal in kv.Value)
                if (portal != null) PoolManager.Instance.Despawn(portal);
        }
        spawnPoints.Clear();
    }

    // 구역 클릭 시: 포탈 위치에 정보 텍스트 프리팹을 띄우고, 그 TMP에 웨이브 정보를 채운다.
    // 월드 오브젝트라 화면을 이동해도 포탈 옆에 그대로 유지된다.
    private void ShowStageInfo(int region)
    {
        if (infoTextPrefab == null) return;
        if (!spawnPoints.TryGetValue(region, out var portals) || portals == null || portals.Count == 0 || portals[0] == null) return; // 포탈 없으면 표시 안 함
        if (!_byRegion.TryGetValue(region, out var spawner) || spawner == null) return;

        Vector3 pos = portals[0].transform.position + infoTextOffset; // 정보 텍스트는 첫 포탈 옆에
        if (!_infoTexts.TryGetValue(region, out var go) || go == null)
        {
            go = PoolManager.Instance.Spawn(infoTextPrefab, pos, Quaternion.identity);
            _infoTexts[region] = go;
        }
        else
        {
            go.transform.position = pos;
        }

        spawner.text = go.GetComponentInChildren<TMP_Text>(true);
        spawner.OnClickStage(region, CurrentDay, UnlockedRegions()); // 텍스트에 웨이브/증원/보스 정보 기록
    }

    private void HideStageInfos()
    {
        foreach (var kv in _infoTexts)
        {
            if (kv.Value != null) PoolManager.Instance.Despawn(kv.Value);
            if (_byRegion.TryGetValue(kv.Key, out var spawner) && spawner != null)
                spawner.text = null; // 반납된 풀 오브젝트를 계속 참조하지 않도록 해제
        }
        _infoTexts.Clear();
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
        {
            IsUnlockregion[region] = true; //해금 할때 씀
            if (changeCheck) ShowPortal(region); // 낮에 확장하면 확장된 곳에 즉시 포탈 생성
        }
        else
        {
            IsUnlockregion[region] = false;
        }
    }

    public bool IsUnlocked(int region)
        => IsUnlockregion.TryGetValue(region, out bool v) && v; //해금 확인용

    // 현재 해금된 지역 번호 목록. 증원 소스로 각 스포너에 넘긴다(스포너가 자기 지역은 알아서 제외).
    private List<int> UnlockedRegions()
    {
        var list = new List<int>();
        foreach (var kv in _byRegion)
            if (IsUnlocked(kv.Key)) list.Add(kv.Key);
        return list;
    }

    public void SpawnWave(int round) //해당라운드 전체소환
    {
        var unlocked = UnlockedRegions();
        foreach (int region in unlocked)
            _byRegion[region].SpawnWave(region, round, unlocked);
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
