using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
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
    // 지역번호 → 그 지역이 해금된 시점의 글로벌 DayCount - 1. LocalStage 계산용 오프셋.
    // 시작 해금 지역(startUnlocked)은 0으로 둬서 기존처럼 글로벌 DayCount와 로컬 진행도가 같게 유지한다.
    private readonly Dictionary<int, int> _unlockOffset = new();
    public ModuleLogic[] moduleLogics;
    public GameObject spawnPoint;
    private float yOffset = 1f;
    // 지역번호 → 그 지역에 이번 라운드 활성화된 포탈들(레인마다 1개).
    public Dictionary<int,List<GameObject>> spawnPoints = new();

    [Tooltip("스테이지 정보 패널(고정 UI). 포탈을 클릭하면 켜지고 그 지역 웨이브로 내용이 채워진다. " +
             "포탈마다 월드 팝업을 스폰하던 방식을 대체한다 — 패널은 하나뿐이고 SetActive로만 껐다 켠다.")]
    [SerializeField] private StageInfoView stageInfoPanel;
    // 지금 패널에 내용을 채워 넣은 지역. 닫을 때 그 스포너의 참조만 끊으면 되므로 들고 있는다.
    private int _shownRegion = -1;
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
        HideStageInfos();
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
        // 팝업(월드 스페이스 캔버스) 위를 클릭한 거면 여기선 아무것도 하지 않는다.
        // 이 가드가 없으면 아이콘을 눌러도 레이가 스폰 타일을 못 맞춰 HideStageInfos()가 돌고
        // 툴팁이 뜨는 같은 프레임에 팝업이 사라진다.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        foreach (var kv in _byRegion)
        {
            WaveSpawner spawner = kv.Value;
            if (spawner == null || spawner.Board == null) continue;

            Tile tile = spawner.Board.CellFromRay(ray);
            if (tile == null || !tile.isEnemySpawn) 
            {
                HideStageInfos();
                continue;
            }
            if(!IsUnlocked(kv.Key))continue;
            ShowStageInfo(kv.Key, tile); // 스테이지 정보 패널을 켜고 그 지역의 웨이브 정보로 채운다
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

    // 지역별 로컬 진행도. 그 지역이 해금된 날을 1일차로 다시 센다 —
    // 나중에 해금된 지역이 글로벌 DayCount 배율을 그대로 물려받아 첫 웨이브부터 몰리는 걸 막는다.
    // 오프셋은 해금되는 그 순간(UnlockRegion 호출 시점)이 아니라, 이 지역 정보가 처음 조회되는 시점에 확정한다.
    // ResultState처럼 UnlockNextModule()이 OnDay()로 DayCount가 오르기 "직전"에 불리는 경로가 있어서,
    // 해금 시점에 바로 계산하면 아직 안 오른 DayCount 기준으로 오프셋이 고정되어 그 지역이 영구히 하루씩 밀린다.
    public int LocalStage(int region)
    {
        if (!_unlockOffset.TryGetValue(region, out int off))
        {
            off = CurrentDay - 1;
            _unlockOffset[region] = off;
        }
        return CurrentDay - off;
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
        HideStageInfos(); // 밤엔 정보 패널을 닫는다
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

        // 켜진 길들 위에 포탈을 세운다 (RestorePortal과 같이 쓰는 헬퍼)
        SpawnPortalsFor(region, paths);
    }

    // 포탈 테이블(Region,Id,Count)에서 이번 지역·라운드에 열 포탈 수를 읽는다.
    // Id는 WaveTable과 같은 라운드 체계(10일차 초과는 1001~1005 순환). 행이 없으면 1개.
    private int PortalCount(int region)
    {
        PortalTable table = DataTableManager.Get<PortalTable>(DataTableIds.Portal);
        if (table == null) return 1;
        int id = WaveSpawner.GetStageLookupId(LocalStage(region));
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

    // 구역 클릭 시: 고정 스테이지 정보 패널을 켜고 그 지역의 웨이브 정보로 채운다.
    // 패널은 씬에 하나뿐이라 지역을 바꿔 클릭하면 같은 패널의 내용만 갈아끼운다.
    private void ShowStageInfo(int region, Tile clickedTile)
    {
        if (stageInfoPanel == null) return;
        if (!spawnPoints.TryGetValue(region, out var portals) || portals == null || portals.Count == 0) return; // 포탈 없으면 표시 안 함
        if (!_byRegion.TryGetValue(region, out var spawner) || spawner == null) return;

        // 클릭한 스폰 칸 위에 실제로 포탈이 서 있는지만 확인한다(포탈은 레인 시작 칸에 세워지므로 좌표가 일치).
        // 이번 라운드에 안 뽑혀 포탈이 없는 칸이면 적이 나오지 않는 자리 — 패널을 띄우지 않고 떠 있던 것도 닫는다.
        if (FindPortalAt(spawner.Board, portals, clickedTile) == null) { HideStageInfos(); return; }

        // 다른 지역을 눌렀으면 이전 지역의 참조부터 끊는다(스포너 하나가 패널을 계속 물고 있지 않게).
        if (_shownRegion >= 0 && _shownRegion != region) UnbindSpawner(_shownRegion);

        stageInfoPanel.Show();
        // 툴팁 스탯이 EnemyBase와 같은 배율을 쓰려면 글로벌 일차가 필요하다.
        // 패널은 컨테이너에 등록돼 있지 않아 GameManager를 직접 주입받지 못하므로 여기서 넘긴다.
        stageInfoPanel.SetDay(CurrentDay);
        spawner.infoView = stageInfoPanel;
        spawner.text = null;   // 패널을 쓰는 동안은 TMP 한 덩어리 폴백을 죽여둔다
        _shownRegion = region;
        spawner.OnClickStage(region, LocalStage(region), UnlockedCount(), CurrentDay); // 웨이브/증원/보스 정보 기록
    }

    // 클릭한 칸과 같은 격자 좌표에 서 있는 포탈을 찾는다. 없으면 null(그 칸은 이번 라운드에 안 뽑힌 스폰 지점).
    private static GameObject FindPortalAt(MapBoard board, List<GameObject> portals, Tile clickedTile)
    {
        if (board == null || clickedTile == null) return null;

        Vector2Int want = clickedTile.Coord;
        foreach (GameObject portal in portals)
        {
            if (portal == null) continue;
            if (board.WorldToCell(portal.transform.position) == want) return portal;
        }
        return null;
    }

    // 패널을 닫는다. 스폰 칸이 아닌 곳을 클릭했을 때와 밤 전환 때 불린다.
    // 패널은 하나뿐이므로 스폰/회수 없이 SetActive만 내린다.
    private void HideStageInfos()
    {
        if (_shownRegion >= 0) UnbindSpawner(_shownRegion);
        _shownRegion = -1;
        if (stageInfoPanel != null) stageInfoPanel.Hide();
    }

    // 패널을 물고 있던 스포너의 참조를 끊는다. 끊기 전에 ResetText로 떠 있던 툴팁/행을 먼저 정리해야
    // 다음에 다른 지역이 같은 패널을 쓸 때 지난 내용이 남지 않는다.
    private void UnbindSpawner(int region)
    {
        if (!_byRegion.TryGetValue(region, out var spawner) || spawner == null) return;
        spawner.ResetText();
        spawner.infoView = null;
        spawner.text = null;
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
        {
            IsUnlockregion[region] = true;
            _unlockOffset[region] = 0; // 글로벌 DayCount와 로컬 진행도를 그대로 일치시킨다
        }
    }

    public void UnlockRegion(int region,ModuleState state)
    {
        if(state!=ModuleState.Locked)
        {
            // 오프셋은 여기서 계산하지 않는다 — LocalStage가 이 지역을 처음 조회하는 시점에 확정한다(위 주석 참고).
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
    public List<int> UnlockedRegions()
    {
        var list = new List<int>();
        foreach (var kv in _byRegion)
            if (IsUnlocked(kv.Key)) list.Add(kv.Key);
        return list;
    }

    // 해금 지역 수를 스포너 밖(EnemyBase의 체력 배율 등)에서 읽기 위한 진입점.
    // Instance 프로퍼티는 매니저가 없으면 빈 오브젝트를 새로 만들어버리므로 여기서는 쓰지 않는다 —
    // 매니저가 없는 씬(적 단독 테스트 등)에서는 0을 돌려주고, 호출부가 배율 1배로 폴백하게 둔다.
    public static int UnlockedRegionCount => instance != null ? instance.UnlockedCount() : 0;

    // 해금된 지역 수. 각 지역의 증원 단계(9001, 9002 …)를 정하는 값이라 스포너에 그대로 넘긴다.
    // 클릭할 때마다 불리므로 목록을 만들지 않고 세기만 한다.
    private int UnlockedCount()
    {
        int count = 0;
        foreach (var kv in _byRegion)
            if (IsUnlocked(kv.Key)) count++;
        return count;
    }

    // 전 지역 해금을 처음 확인한 라운드(=DayCount). 아직이면 -1.
    // 해금되는 순간(UnlockRegion)에 잡지 않고 처음 조회되는 시점에 확정하는 이유는 _unlockOffset과 같다 —
    // UnlockNextModule()이 OnDay()로 DayCount가 오르기 "직전"에 불리는 경로가 있어서,
    // 그때 잡으면 아직 안 오른 DayCount로 굳어 기준이 영구히 하루 밀린다.
    private int _fullUnlockDay = -1;

    /// <summary>
    /// 전 지역이 해금된 뒤 지난 라운드 수(=DayCount 차이). 아직 다 안 열렸으면 0.
    /// 지역 해금 배율이 표 마지막 칸에서 멈춘 뒤에도 보스가 계속 세지게 하는 데 쓴다
    /// (EnemyBase.BossFullUnlockHpBonus). 매니저가 없는 씬에서는 0이라 호출부가 보너스 없이 폴백한다.
    /// </summary>
    public static int RoundsSinceFullUnlock => instance != null ? instance.RoundsSinceFullUnlockInternal() : 0;

    private int RoundsSinceFullUnlockInternal()
    {
        // 스포너가 아직 안 모였으면 "전부 해금"을 0개 중 0개로 착각해 참이 되어버린다 — 그 전엔 0을 돌려준다.
        if (_byRegion.Count == 0) return 0;
        if (UnlockedCount() < _byRegion.Count) return 0;

        if (_fullUnlockDay < 0) _fullUnlockDay = CurrentDay;
        return Mathf.Max(0, CurrentDay - _fullUnlockDay);
    }

    public void SpawnWave(int round) //해당라운드 전체소환 (round는 GameManager.DayCount와 항상 같음 — 지역별 진행도는 LocalStage로 따로 계산)
    {
        var unlocked = UnlockedRegions();
        // 웨이브 조합(어떤 몹이 나올지)은 지역별 LocalStage, 마릿수 배율은 글로벌 DayCount 기준으로 유지한다.
        foreach (int region in unlocked)
            _byRegion[region].SpawnWave(region, LocalStage(region), unlocked.Count, CurrentDay);
    }


    public void SpawnRegion(int region,int round) //해당 지역 라운드 소환(테스트 용)
    {
        if (!IsUnlocked(region)) return;
        if (_byRegion.TryGetValue(region, out WaveSpawner s))
            s.SpawnWave(region, LocalStage(region), UnlockedCount(), CurrentDay);
    }
    private void OnRegionClear() //몹 다잡았을때
    {
        foreach (var kv in _byRegion)
            if (IsUnlocked(kv.Key) && kv.Value.Enemycount > 0)
                return; // 아직 남은 지역이 있음

        AllRegionsClear?.Invoke();
    }
    
    // 길들에서 포탈을 하나씩 만들어 세운다. ShowPortal(낮에 켤 때)과 RestorePortal(로드할 때) 둘 다 이걸 쓴다.
    private void SpawnPortalsFor(int region, IReadOnlyList<IReadOnlyList<Vector3>> paths)
    {
        // 포탈을 담을 목록을 길 개수만큼 미리 만든다
        var portals = new List<GameObject>(paths.Count);
        // 포탈을 땅 위로 살짝 띄우는 높이값
        Vector3 lift = Vector3.up * yOffset;
        // 길 하나마다 그 길의 시작점 위에 포탈을 하나 꺼내 세운다 (풀에서 재사용)
        foreach (var path in paths)
        {
            portals.Add(PoolManager.Instance.Spawn(spawnPoint, path[0] + lift, Quaternion.identity));
        }
        // 이 지역의 포탈 목록으로 기록해둔다 (나중에 지우거나 찾을 때 씀)
        spawnPoints[region] = portals;
    }

    // 이 지역이 전체 날짜보다 며칠 늦게 시작했는지(오프셋)를 읽는다. 세이브할 때 부른다.
    public int GetOffset(int region)
    {
        // 이미 정해진 오프셋이 있으면 그 값을 그대로 돌려준다
        if (_unlockOffset.TryGetValue(region, out int off)) return off;
        // 아직 안 열린 지역이면 오프셋이 필요 없으니 0을 돌려준다
        if (!IsUnlocked(region)) return 0;

        // 열려있는데 값이 없으면, 지금 날짜 기준으로 새로 정해서 저장해둔다
        off = CurrentDay - 1;
        _unlockOffset[region] = off;
        return off;
    }

    // 저장해뒀던 오프셋 숫자를 그대로 다시 집어넣는다. 로드할 때 부른다.
    public void RestoreOffset(int region, int offset)
    {
        // 계산 없이 저장된 값 그대로 넣는다
        _unlockOffset[region] = offset;
    }

    // 지금 켜져 있는 포탈들의 칸 좌표를 읽어온다. 세이브할 때 부른다.
    public bool TryGetActiveSpawnCoords(int region, out Vector2Int[] coords)
    {
        // 일단 빈 값으로 시작해둔다
        coords = Array.Empty<Vector2Int>();
        // 이 지역 담당 스포너가 없으면 읽을 게 없다
        if (!_byRegion.TryGetValue(region, out var spawner) || spawner == null) return false;

        // 지금 켜진 포탈들의 번호 목록
        IReadOnlyList<int> activeSpawns = spawner.ActiveSpawns;
        // 번호마다 실제 칸 좌표를 찾아서 담는다
        coords = new Vector2Int[activeSpawns.Count];
        for (int i = 0; i < activeSpawns.Count; i++)
        {
            coords[i] = spawner.SpawnCoord(activeSpawns[i]);
        }
        return true;
    }

    // 저장된 좌표대로 포탈을 다시 세운다. 로드할 때 부른다.
    public void RestorePortal(int region, Vector2Int[] savedCoords)
    {
        // 포탈을 세울 기준 오브젝트가 없으면 아무것도 못 하니 끝낸다
        if (spawnPoint == null) return;
        // 이 지역 담당 스포너를 못 찾으면 끝낸다
        if (!_byRegion.TryGetValue(region, out var spawner) || spawner == null) return;

        // 혹시 이미 떠 있는 포탈이 있으면(중복 방지) 먼저 다 지운다
        if (spawnPoints.TryGetValue(region, out var existing) && existing != null)
        {
            // 떠 있던 포탈들을 하나씩 반납한다
            foreach (var portal in existing)
                if (portal != null) PoolManager.Instance.Despawn(portal);
            // 이 지역의 포탈 기록도 지운다
            spawnPoints.Remove(region);
        }

        // 저장된 좌표대로 길을 켠다
        spawner.ActivateSpawnsAt(savedCoords);

        // 켜진 길들 위에 포탈을 세운다
        SpawnPortalsFor(region, spawner.ActivePaths);
    }
     [SerializeField] private GameObject guardPanel;
    [SerializeField] private Animator directingUi;
    [SerializeField] private string directingStateName = "Directing";
    [SerializeField] private float directingTimeout = 5f;
    public bool isDirecting = false;
    public async UniTask BossOpeningDirecting(CancellationToken token, float bossDelay = 0f)
    {
        if (bossDelay > 0f)
            await UniTask.Delay(TimeSpan.FromSeconds(bossDelay), cancellationToken: token);

        float savedTimeScale = Time.timeScale;
        AnimatorUpdateMode savedUpdateMode = directingUi != null ? directingUi.updateMode : AnimatorUpdateMode.Normal;
        try
        {
            guardPanel?.SetActive(true);
            Time.timeScale = 0f;
            isDirecting = true;

            if (directingUi != null)
            {
                // 기본 Normal 모드는 timeScale을 따라가므로 얼린 동안 재생되게 언스케일드로 전환.
                directingUi.updateMode = AnimatorUpdateMode.UnscaledTime;
                directingUi.Rebind();   // 재사용되는 오브젝트라 지난 연출 끝난 지점이 아니라 처음부터 다시 재생
                directingUi.Update(0f);
                directingUi.SetTrigger(directingStateName);
                await WaitForDirectingAnim(directingUi, directingStateName, directingTimeout, token);
            }
        }
        catch (OperationCanceledException)
        {
            // 씬 전환/취소 — 정상 취소이므로 무시하고 finally에서 원복만 한다.
        }
        finally
        {
            if (directingUi != null) directingUi.updateMode = savedUpdateMode;
            Time.timeScale = savedTimeScale;
            guardPanel?.SetActive(false);
            isDirecting = false;
        }
    }

    // EnemyBase.WaitForAttackAnim과 같은 패턴이지만, 이 연출은 Time.timeScale=0인 동안 돌아가므로
    // 시간 소스를 전부 언스케일드로 맞춘다 — 스케일드로 두면(Time.deltaTime, UniTask.Delay 기본값)
    // 얼려있는 동안 delta가 계속 0이라 영원히 안 끝난다.
    private static async UniTask WaitForDirectingAnim(Animator anim, string stateName, float timeout, CancellationToken token)
    {
        const int layer = 0;
        float elapsed = 0f;
        while (anim != null && !anim.GetCurrentAnimatorStateInfo(layer).IsName(stateName))
        {
            elapsed += Time.unscaledDeltaTime;
            if (elapsed >= timeout)
            {
                Debug.LogWarning($"BossOpeningDirecting: Animator에서 '{stateName}' 스테이트를 찾지 못함 — 이름/전이 확인.", anim);
                return;
            }
            await UniTask.Yield(token);
        }
        if (anim == null) return;

        var info = anim.GetCurrentAnimatorStateInfo(layer);
        float wait = info.length / Mathf.Max(0.01f, anim.speed);
        await UniTask.Delay(TimeSpan.FromSeconds(wait), ignoreTimeScale: true, cancellationToken: token);
    }
}
