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
    public TMP_Text text;
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

    // 스폰 타일별 전체 경로(맵당 1회 캐시). GetPaths가 스폰당 1경로를 준다.
    private IReadOnlyList<IReadOnlyList<Vector3>> _allPaths;
    // 이번 라운드에 활성화된 경로들. 포탈 표시(낮)와 적 경로 배분(밤)이 이 집합을 공유한다.
    private readonly List<IReadOnlyList<Vector3>> _activePaths = new();
    // 활성 레인 추첨용 임시 버퍼(GC 회피).
    private readonly List<IReadOnlyList<Vector3>> _laneBuffer = new();
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
        _allPaths = enemyLanes.GetPaths(0f);
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

        _laneBuffer.Clear();
        if (_allPaths != null)
            for (int i = 0; i < _allPaths.Count; i++)
                if (_allPaths[i] != null && _allPaths[i].Count > 0) _laneBuffer.Add(_allPaths[i]);

        if (_laneBuffer.Count == 0) // 레인 정보 없음 → 단일 경로 폴백
        {
            if (waypoints != null && waypoints.Count > 0) _activePaths.Add(waypoints);
            return _activePaths.Count;
        }

        int take = Mathf.Clamp(count, 1, _laneBuffer.Count);
        for (int i = 0; i < take; i++) // Fisher-Yates 부분 셔플로 take개 뽑기
        {
            int j = UnityEngine.Random.Range(i, _laneBuffer.Count);
            (_laneBuffer[i], _laneBuffer[j]) = (_laneBuffer[j], _laneBuffer[i]);
            _activePaths.Add(_laneBuffer[i]);
        }
        return _activePaths.Count;
    }

    // 코어에서 가장 먼(=경로가 가장 긴) 스폰 레인 하나만 고정 활성화한다. 보스 라운드의 "끝 구석" 포탈용.
    // 경로는 스폰→코어 순서라 Count가 클수록 코어에서 멀다. 동률이면 앞선 레인(좌표순 정렬) → 결정적.
    public int ActivateCornerPortal()
    {
        EnsurePaths();
        _activePaths.Clear();

        IReadOnlyList<Vector3> corner = null;
        int best = -1;
        if (_allPaths != null)
            for (int i = 0; i < _allPaths.Count; i++)
            {
                var p = _allPaths[i];
                if (p == null || p.Count == 0) continue;
                if (p.Count > best) { best = p.Count; corner = p; }
            }

        if (corner == null) corner = waypoints;                 // 레인 정보 없으면 단일 경로 폴백
        if (corner != null && corner.Count > 0) _activePaths.Add(corner);
        return _activePaths.Count;
    }

    // 활성 포탈 중 하나를 랜덤으로 골라 그 경로를 준다. 활성 집합이 비면 단일 경로 폴백.
    private IReadOnlyList<Vector3> NextSpawnPath()
    {
        if (_activePaths.Count == 0) return waypoints;
        return _activePaths[UnityEngine.Random.Range(0, _activePaths.Count)];
    }
    public void ResetText()
    {
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

    public void OnClickStage(int region,int currentstage, IEnumerable<int> reinforcementSources = null)
    {
        if (waveTable == null || text == null) return; // Start 전 클릭/텍스트 미할당 방어
        text.text = $"{region}지역 {currentstage}일차\n";
        int lookupId = GetStageLookupId(currentstage);
        foreach(var w in waveTable.GetWave(region,lookupId))
        {
            int count = GetScaleCount(w.Count, currentstage);
            text.text += $"{DataTableManager.StringTable.Get(w.MonsterName)} {count}마리 \n";
        }
        if (reinforcementSources != null)
        {
            foreach (int src in reinforcementSources)
            {
                if (src == region) continue;
                foreach (var w in waveTable.GetWave(src, ReinforceId))
                {
                    int count = GetScaleCount(w.Count, currentstage);
                    text.text += $"{DataTableManager.StringTable.Get(w.MonsterName)} {count}마리 (증원)\n";
                }
            }
        }
        if(currentstage>10&&currentstage%10==0&&region==1)
        {
            foreach(var w in waveTable.GetWave(1,10))
            {
                text.text +=$"(보스){DataTableManager.StringTable.Get(w.MonsterName)} {w.Count}마리";
            }
        }
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
