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
    public IReadOnlyList<Vector3> waypoints; // 스폰→본진 경로. 맵당 1회 계산해 모든 적이 공유.

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
            waypoints = board.GetWaypoints(0f); 
            EnemyGridService.mapBoard = board;
        }
       ResetText();
    }
    public void ResetText()
    {
        if(text == null || string.IsNullOrEmpty(text.text))return;
        
        text.text = string.Empty;
    }
    public void SpawnWave(int currentStage)
    {
        Enemycount =0;
        int lookupId = GetStageLookupId(currentStage); // 10일차 초과는 1001~1005 라운드로 순환 조회
        foreach(var wave in waveTable.GetWave(1,lookupId))
        {
            int count = GetScaleCount(wave.Count, currentStage); // 라운드가 돌수록 마릿수 스케일업
            SpawnWaveRout(wave, count).Forget();
            Enemycount += count;
        }
        Debug.Log($"{region}지역 : {Enemycount}");
    }
    // reinforcementSources: 해금된 "다른" 지역 번호들. 그 지역이 export하는 증원 몹(9001 대역)을 이 지역 웨이브에 추가로 얹는다.
    public void SpawnWave(int region,int currentStage, IEnumerable<int> reinforcementSources = null)
    {
        Enemycount =0;
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
        Debug.Log($"{region}지역 총마릿수 : {Enemycount}");
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
                enemy.EnterMap(board, waypoints);
            }

            if (i < count - 1 && wave.Delay > 0f)
            await UniTask.Delay(TimeSpan.FromSeconds(wave.Delay));
        }
    }
    public void EnemyDieEvent()
    {
        Enemycount--;
        Debug.Log($"{region}지역남은 마릿수 : {Enemycount}");
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
