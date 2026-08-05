using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using VContainer;

public class WaveSpawner : MonoBehaviour
{

    [Tooltip("적이 따라갈 격자 맵. 인스펙터에서 주입(Find 함수 미사용 지침).")]
    [SerializeField] private MapBoard board;
    public MapBoard Board => board;
    [Tooltip("이 스포너가 담당하는 지역(레인) 번호. SpawnerManager가 이 값으로 매핑한다.")]
    [SerializeField] private int region = 1;
    public int Region => region;
    public int Enemycount;
    [Tooltip("구버전 표시용 폴백. 팝업 프리팹에 StageInfoView가 붙어 있으면 쓰이지 않는다.")]
    public TMP_Text text;
    [Tooltip("스테이지 정보 팝업(아이콘 행 + 툴팁). SpawnerManager가 클릭 때마다 넣어준다.")]
    public StageInfoView infoView;
    [Tooltip("보스 라운드에서 일반 몹은 즉시, 보스는 이 시간(초) 뒤에 등장.")]
    [SerializeField] private float bossSpawnDelay = 10f;
    
    private WaveTable waveTable;
    public IReadOnlyList<Vector3> waypoints; // 단일 경로 폴백(레인 정보가 없을 때만 사용).

    [Tooltip("스폰 타일별 경로(레인)를 제공. board와 같은 오브젝트에 붙는다. 비어 있으면 런타임에 자동 부착.")]
    [SerializeField] private EnemyLanes enemyLanes;
    [Tooltip("이번 라운드에 활성화할 포탈(레인) 최소 개수.")]
    [SerializeField] private int minActivePortals = 1;
    [Tooltip("이번 라운드에 활성화할 포탈(레인) 최대 개수.")]
    [SerializeField] private int maxActivePortals = 3;

    // 스폰 타일별 전체 경로(날짜가 바뀌기 전까지 캐시). GetPaths가 스폰당 1경로를 준다.
    private IReadOnlyList<IReadOnlyList<Vector3>> _allPaths;
    // enemyLanes.Changed 중복 구독 방지용. EnsurePaths가 캐시 히트로 일찍 끝나도 한 번만 걸린다.
    private bool _hookedLaneChanges;
    // 이번 라운드에 활성화된 경로들. 포탈 표시(낮)와 적 경로 배분(밤)이 이 집합을 공유한다.
    private readonly List<IReadOnlyList<Vector3>> _activePaths = new();
    // 이번 라운드에 활성화된 포탈(스폰) 번호. _activePaths와 같은 순서라야 그 포탈의 갈래를 고를 수 있다.
    public IReadOnlyList<int> ActiveSpawns => _activeSpawns;
    // 활성 레인 추첨용 임시 버퍼(GC 회피).
    private readonly List<IReadOnlyList<Vector3>> _laneBuffer = new();
    // 활성 포탈의 스폰 번호. _activePaths와 같은 순서라야 그 포탈의 갈래를 고를 수 있다.
    private readonly List<int> _activeSpawns = new();
    private readonly List<int> _spawnBuffer = new();
    /// <summary>이번 라운드에 켜진 포탈(레인)들의 경로. 각 경로 [0]이 포탈 위치.</summary>
    public IReadOnlyList<IReadOnlyList<Vector3>> ActivePaths => _activePaths;

    // 스코프에 등록되면 주입됨. 아니면 Spawn 시 Instance로 폴백.
    private PoolManager _pool;
    private SpawnerManager spawnerManager;
    [Inject] public void Construct(PoolManager pool,SpawnerManager spawnerManager)
    {
        _pool = pool;
        this.spawnerManager = spawnerManager;
    }
    
    public event Action EnemyAllClear;
    private void Start()
    {
        waveTable = DataTableManager.Get<WaveTable>(DataTableIds.Wave);
        if (waveTable == null)
        {
            Debug.LogWarning("WaveSpawner: WaveTable을 찾을 수 없음");
            return;
        }

        if (board == null)
            Debug.LogWarning("WaveSpawner: MapBoard가 주입되지 않았습니다. 적이 이동하지 않습니다.", this);
        else
        {
            waypoints = board.GetWaypoints(0f); // 레인 정보가 없을 때 쓰는 단일 경로 폴백
            EnsurePaths();                      // 스폰 타일별 경로(레인) 캐시
            EnemyGridService.mapBoard = board;
        }
       ResetText();
    }

    // 스폰 타일별 경로(레인)를 확보한다. EnemyLanes가 없으면 board에 자동 부착한다.
    // EnemyLanes.Awake(실행순서 100)가 board.Awake 이후 레인을 굽는다 → 어떤 Start 순서에서도 안전.
    private void EnsurePaths()
    {
        if (_allPaths != null || board == null) return;
        if (enemyLanes == null)
            enemyLanes = board.GetComponent<EnemyLanes>() ?? board.gameObject.AddComponent<EnemyLanes>();
        if (!_hookedLaneChanges)
        {
            enemyLanes.Changed += OnLanesChanged; // 날짜가 바뀌어 레인이 다시 구워지면 캐시를 버리고 다시 받는다
            _hookedLaneChanges = true;
        }
        _allPaths = enemyLanes.GetPaths(0f);
    }

    // EnemyLanes가 레인을 다시 구울 때(날짜 변경 등) 호출된다. 캐시를 비우고 그 자리에서 새 경로로 다시 채운다.
    private void OnLanesChanged()
    {
        _allPaths = null;
        EnsurePaths();
    }

    private void OnDestroy()
    {
        if (enemyLanes != null)
            enemyLanes.Changed -= OnLanesChanged;
    }

    // 이번 라운드에 켤 포탈(레인)을 min~max 범위에서 랜덤 개수만큼 활성화한다.
    // 낮에 포탈을 세우기 직전(SpawnerManager)에 호출 → 그 집합을 밤 스폰까지 그대로 쓴다.
    public int RollActivePortals()
        => RollActivePortals(UnityEngine.Random.Range(minActivePortals, maxActivePortals + 1));

    // count개의 레인을 랜덤으로 활성화한다. 유효 경로가 count보다 적으면 있는 만큼만.
    // 반환: 실제 활성화된 포탈 수.
    public int RollActivePortals(int count)
    {
        EnsurePaths();
        _activePaths.Clear();
        _activeSpawns.Clear();

        _laneBuffer.Clear();
        _spawnBuffer.Clear();
        if (_allPaths != null)
            for (int i = 0; i < _allPaths.Count; i++)
                if (_allPaths[i] != null && _allPaths[i].Count > 0) { _laneBuffer.Add(_allPaths[i]); _spawnBuffer.Add(i); }

        if (_laneBuffer.Count == 0) // 레인 정보 없음 → 단일 경로 폴백
        {
            if (waypoints != null && waypoints.Count > 0) { _activePaths.Add(waypoints); _activeSpawns.Add(NoSpawn); }
            return _activePaths.Count;
        }

        int take = Mathf.Clamp(count, 1, _laneBuffer.Count);
        for (int i = 0; i < take; i++) // Fisher-Yates 부분 셔플로 take개 뽑기
        {
            int j = UnityEngine.Random.Range(i, _laneBuffer.Count);
            (_laneBuffer[i], _laneBuffer[j]) = (_laneBuffer[j], _laneBuffer[i]);
            (_spawnBuffer[i], _spawnBuffer[j]) = (_spawnBuffer[j], _spawnBuffer[i]);
            _activePaths.Add(_laneBuffer[i]);
            _activeSpawns.Add(_spawnBuffer[i]);
        }
        return _activePaths.Count;
    }

    // 코어에서 가장 먼(=경로가 가장 긴) 스폰 레인 하나만 고정 활성화한다. 보스 라운드의 "끝 구석" 포탈용.
    // 경로는 스폰→코어 순서라 Count가 클수록 코어에서 멀다. 동률이면 앞선 레인(좌표순 정렬) → 결정적.
    public int ActivateCornerPortal()
    {
        EnsurePaths();
        _activePaths.Clear();
        _activeSpawns.Clear();

        IReadOnlyList<Vector3> corner = null;
        int best = -1;
        int cornerSpawn = NoSpawn;
        if (_allPaths != null)
            for (int i = 0; i < _allPaths.Count; i++)
            {
                var p = _allPaths[i];
                if (p == null || p.Count == 0) continue;
                if (p.Count > best) { best = p.Count; corner = p; cornerSpawn = i; }
            }

        if (corner == null) { corner = waypoints; cornerSpawn = NoSpawn; } // 레인 정보 없으면 단일 경로 폴백
        if (corner != null && corner.Count > 0) { _activePaths.Add(corner); _activeSpawns.Add(cornerSpawn); }
        return _activePaths.Count;
    }

    // 레인 정보 없이 켠 폴백 경로라 스폰 번호가 없다는 표시.
    private const int NoSpawn = -1;

    // 활성 포탈 중 하나를 랜덤으로 고르고, 그 포탈의 갈래 중 하나를 준다. 활성 집합이 비면 단일 경로 폴백.
    private IReadOnlyList<Vector3> NextSpawnPath()
    {
        if (_activePaths.Count == 0) return waypoints;
        return BranchPath(UnityEngine.Random.Range(0, _activePaths.Count));
    }

    // 이 포탈에서 갈라지는 길 중 하나. 갈래가 하나뿐이거나 막혀 있으면 대표 경로 그대로.
    private IReadOnlyList<Vector3> BranchPath(int pick)
    {
        int spawn = _activeSpawns[pick];
        if (enemyLanes == null || spawn == NoSpawn) return _activePaths[pick];

        int branches = enemyLanes.BranchCount(spawn);
        if (branches <= 1) return _activePaths[pick];

        var branch = enemyLanes.GetBranchPath(spawn, UnityEngine.Random.Range(0, branches), 0f);
        if (branch == null || branch.Count == 0) return _activePaths[pick];
        return branch;
    }
    public void ResetText()
    {
        if (infoView != null) infoView.Clear();
        if(text == null || string.IsNullOrEmpty(text.text))return;

        text.text = string.Empty;
    }
    public void SpawnWave(int currentStage)
    {
        Enemycount =0;
        if (_activePaths.Count == 0) RollActivePortals(); // 포탈 추첨 없이 스폰되면(테스트 등) 여기서 보정
        int lookupId = GetStageLookupId(currentStage); // 10일차 초과는 1001~1005 라운드로 순환 조회
        foreach(var wave in waveTable.GetWave(1,lookupId))
        {
            int count = GetScaleCount(wave.Count, currentStage); // 라운드가 돌수록 마릿수 스케일업
            SpawnWaveRout(wave, count).Forget();
            Enemycount += count;
        }
    }
    // reinforcementSources: 해금된 "다른" 지역 번호들. 그 지역이 export하는 증원 몹(9001 대역)을 이 지역 웨이브에 추가로 얹는다.
    public void SpawnWave(int region,int currentStage, IEnumerable<int> reinforcementSources = null)
    {
        Enemycount =0;
        if (_activePaths.Count == 0) RollActivePortals(); // 포탈 추첨 없이 스폰되면 여기서 보정
        int lookupId = GetStageLookupId(currentStage);
        foreach(var wave in waveTable.GetWave(region,lookupId))
        {
            int count = GetScaleCount(wave.Count, currentStage); // 라운드가 돌수록 마릿수 스케일업
            SpawnWaveRout(wave, count).Forget();
            Enemycount += count;
        }
        if (reinforcementSources != null)
        {
            foreach (int src in reinforcementSources)
            {
                if (src == region) continue; // 자기 자신 제외
                foreach (var wave in waveTable.GetWave(src, ReinforceId))
                {
                    SpawnWaveRout(wave, wave.Count).Forget();
                    Enemycount += wave.Count;
                }
            }
        }
        
        if (region == 1 && currentStage > 10 && currentStage % 10 == 0)
        {
            foreach (var w in waveTable.GetWave(1, 10))
            {
                SpawnWaveRout(w, w.Count, bossSpawnDelay).Forget();
                Enemycount += w.Count;
            }
        }
    }

    private async UniTask SpawnWaveRout(WaveTable.Data wave, int count, float startDelay = 0f)
    {
        var prefab = waveTable.GetMonsterPrefab(wave);
        if (prefab == null)
        {
            Debug.LogWarning($"WaveSpawner: 프리팹 로드 실패 '{wave.Prefab}' (ID {wave.ID})");
            return;
        }
        // 시작 지연(보스 지연 등) → 그 위에 웨이브별 SpawnTime을 더한다.
        if (startDelay > 0f) await UniTask.Delay(TimeSpan.FromSeconds(startDelay));
        if (wave.SpawnTime > 0f) await UniTask.Delay(TimeSpan.FromSeconds(wave.SpawnTime));

        for (int i = 0; i < count; i++)
        {

            var go = (_pool ??= PoolManager.Instance).Spawn(prefab, Vector3.zero, Quaternion.identity);
            if (go.TryGetComponent(out EnemyBase enemy))
            {
                enemy.SetOwner(this);
                enemy.EnterMap(board, NextSpawnPath()); // 활성 포탈 중 랜덤 경로 배분
            }

            if (i < count - 1 && wave.Delay > 0f)
            await UniTask.Delay(TimeSpan.FromSeconds(wave.Delay));
        }
    }
    public void EnemyDieEvent()
    {
        Enemycount--;
        if(Enemycount<=0)
        {
            EnemyAllClear?.Invoke();
            Debug.Log("적 전멸 이벤트 발생");
        }
    }

    public void AddSpawnCount(int n) => Enemycount += n;

    // 스테이지 정보 표시. infoView가 있으면 적 아이콘 + x마릿수 행으로, 없으면 예전처럼 텍스트 한 덩어리로 쓴다.
    // (프리팹에 StageInfoView를 아직 붙이지 않은 상태에서도 게임이 돌아가도록 폴백을 남겨둠)
    public void OnClickStage(int region,int currentstage, IEnumerable<int> reinforcementSources = null)
    {
        if (waveTable == null) return;                          // Start 전 클릭 방어
        if (infoView == null && text == null) return;           // 표시할 대상이 아무것도 없음

        int lookupId = GetStageLookupId(currentstage);
        // 이번 클릭 내용만 남게 매번 비우고 시작한다(안 비우면 클릭할수록 목록이 쌓인다).
        if (infoView != null) infoView.Begin();
        else text.text = string.Empty;

        foreach(var w in waveTable.GetWave(region,lookupId))
            AddStageLine(w.MonsterName, GetScaleCount(w.Count, currentstage), null);

        if (reinforcementSources != null)
        {
            foreach (int src in reinforcementSources)
            {
                if (src == region) continue;
                foreach (var w in waveTable.GetWave(src, ReinforceId))
                    AddStageLine(w.MonsterName, GetScaleCount(w.Count, currentstage), "Ui_Add");
            }
        }
        if(currentstage>10&&currentstage%10==0&&region==1)
        {
            foreach(var w in waveTable.GetWave(1,10))
                AddStageLine(w.MonsterName, w.Count, "Ui_Boss");
        }
    }

    // 적 한 종류를 한 줄로 추가. badgeKey: "Ui_Add"(증원) / "Ui_Boss"(보스) / null(일반).
    // WaveTable.MonsterName과 EnemyTable.Name은 같은 키라 그대로 조회한다(아이콘·설명도 이 키 기준).
    private void AddStageLine(string monsterName, int count, string badgeKey)
    {
        if (infoView != null)
        {
            EnemyTable.Data data = DataTableManager.EnemyTable?.Get(monsterName);
            if (data != null) { infoView.AddRow(data, count, badgeKey); return; }
            // EnemyTable에 행이 없는 몹은 아이콘/설명을 만들 수 없다 → 이름만이라도 남긴다.
            Debug.LogWarning($"WaveSpawner: EnemyTable에 '{monsterName}' 없음 — 이름만 표시");
        }
        if (text == null) return;

        var st = DataTableManager.StringTable;
        string badge = string.IsNullOrEmpty(badgeKey) ? string.Empty : $"({st.Get(badgeKey)})";
        text.text += $"{st.Get(monsterName)} x {count}{badge}\n";
    }

    public static int GetScaleCount(int baseCount,int currentStage)
    {
        if(currentStage<=10)return baseCount;
        int loops = (currentStage-6)/5;
        return Mathf.RoundToInt(baseCount*(1f+loops*0.3f));
    }
    public static int GetStageLookupId(int stage)
    {
        return stage > 10 ? ((stage-6)%5)+1001 : stage;
    }

    // 지역 해금 시 다른 해금 지역에 흘려보내는 "증원 몹" 전용 ID 대역.
    // 일반 라운드(1~10, 1001~1005)와 겹치지 않으므로 정상 웨이브로는 절대 스폰되지 않는다.
    public const int ReinforceId = 9001;
}
