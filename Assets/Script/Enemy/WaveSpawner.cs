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
    private WaveTable waveTable;
    public IReadOnlyList<Vector3> waypoints; // 스폰→본진 경로. 맵당 1회 계산해 모든 적이 공유.

    // 스코프에 등록되면 주입됨. 아니면 Spawn 시 Instance로 폴백.
    private PoolManager _pool;
    [Inject] public void Construct(PoolManager pool)
    {
        _pool = pool;
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
    public void SpawnWave(int region,int currentStage)
    {
        Enemycount =0;
        int lookupId = GetStageLookupId(currentStage); // 10일차 초과는 1001~1005 라운드로 순환 조회
        foreach(var wave in waveTable.GetWave(region,lookupId))
        {
            int count = GetScaleCount(wave.Count, currentStage); // 라운드가 돌수록 마릿수 스케일업
            SpawnWaveRout(wave, count).Forget();
            Enemycount += count;
        }
        Debug.Log($"{region}지역 총마릿수 : {Enemycount}");
    }

    private async UniTask SpawnWaveRout(WaveTable.Data wave, int count)
    {
        var prefab = waveTable.GetMonsterPrefab(wave);
        if (prefab == null)
        {
            Debug.LogWarning($"WaveSpawner: 프리팹 로드 실패 '{wave.Prefab}' (ID {wave.ID})");
            return;
        }
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

    public void OnClickStage(int region,int currentstage)
    {
        if (waveTable == null || text == null) return; // Start 전 클릭/텍스트 미할당 방어
        text.text = $"{region}지역 {currentstage}일차\n";
        int lookupId = GetStageLookupId(currentstage);
        foreach(var w in waveTable.GetWave(region,lookupId))
        {
            int count = GetScaleCount(w.Count, currentstage);
            text.text += $"{DataTableManager.StringTable.Get(w.MonsterName)} {count}마리 \n";
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
}
