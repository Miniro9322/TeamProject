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
    [Tooltip("이 스포너가 담당하는 지역(레인) 번호. SpawnerManager가 이 값으로 매핑한다.")]
    [SerializeField] private int region = 1;
    public int Region => region;
    public int Enemycount;
    public TMP_Text text;
    private WaveTable waveTable;
    private IReadOnlyList<Vector3> waypoints; // 스폰→본진 경로. 맵당 1회 계산해 모든 적이 공유.

    // 스코프에 등록되면 주입됨. 아니면 Spawn 시 Instance로 폴백.
    private PoolManager _pool;
    [Inject] public void Construct(PoolManager pool) => _pool = pool;
    
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
        foreach(var wave in waveTable.GetWave(1,currentStage))
        {
            SpawnWaveRout(wave).Forget();
            Enemycount += wave.Count;
        }
        Debug.Log($"총마릿수 : {Enemycount}");
    }
    public void SpawnWave(int region,int currentStage)
    {
        Enemycount =0;
        foreach(var wave in waveTable.GetWave(region,currentStage))
        {
            SpawnWaveRout(wave).Forget();
            Enemycount += wave.Count;
        }
        Debug.Log($"{region}지역 총마릿수 : {Enemycount}");
    }

    private async UniTask SpawnWaveRout(WaveTable.Data wave)
    {
        var prefab = waveTable.GetMonsterPrefab(wave);
        if (prefab == null)
        {
            Debug.LogWarning($"WaveSpawner: 프리팹 로드 실패 '{wave.Prefab}' (ID {wave.ID})");
            return;
        }
        if (wave.SpawnTime > 0f) await UniTask.Delay(TimeSpan.FromSeconds(wave.SpawnTime));

        for (int i = 0; i < wave.Count; i++)
        {
            
            var go = (_pool ??= PoolManager.Instance).Spawn(prefab, Vector3.zero, Quaternion.identity);
            if (go.TryGetComponent(out EnemyBase enemy))
            {
                enemy.SetOwner(this);
                enemy.EnterMap(board, waypoints);
            }

            if (i < wave.Count - 1 && wave.Delay > 0f)
            await UniTask.Delay(TimeSpan.FromSeconds(wave.Delay));
        }
    }
    public void EnemyDieEvent()
    {
        Enemycount--;
        Debug.Log($"남은 마릿수 : {Enemycount}");
        if(Enemycount<=0)
        {
            EnemyAllClear?.Invoke();
        }
    }

    // 분열 등으로 런타임에 추가로 생긴 적을 카운트에 반영(스폰 시점에 호출).
    // 이렇게 미리 더해두면, 그 분열체가 죽을 때 EnemyDieEvent 감소와 상쇄되어 전멸 시 정확히 0이 된다.
    public void AddSpawnCount(int n) => Enemycount += n;
    public void Waveinformation(int currentSatge)
    {
        text.text = $"1지역 스테이지 {currentSatge}\n";
        foreach(var w in waveTable.GetWave(1,currentSatge))
        {
            text.text += $"{DataTableManager.StringTable.Get(w.MonsterName)} {w.Count}마리 \n";
        }
    }
    public void WaveinformationSecond(int currentSatge)
    {
        text.text = $"2지역 스테이지 {currentSatge}\n";
        foreach(var w in waveTable.GetWave(2,currentSatge))
        {
            text.text += $"{DataTableManager.StringTable.Get(w.MonsterName)} {w.Count}마리 \n";
        }
    }
    public void Waveinformation3rd(int currentSatge)
    {
        text.text = $"3지역 스테이지 {currentSatge}\n";
        foreach(var w in waveTable.GetWave(3,currentSatge))
        {
            text.text += $"{DataTableManager.StringTable.Get(w.MonsterName)} {w.Count}마리 \n";
        }
    }
}
