using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{

    [Tooltip("적이 따라갈 격자 맵. 인스펙터에서 주입(Find 함수 미사용 지침).")]
    [SerializeField] private MapBoard board;

    private WaveTable waveTable;
    private IReadOnlyList<Vector3> waypoints; // 스폰→본진 경로. 맵당 1회 계산해 모든 적이 공유.

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
            waypoints = board.GetWaypoints(0f); // 타일 윗면 기준. 유닛별 높이는 EnemyBase가 더함.
            EnemyGridService.mapBoard = board;  // 공격범위 판정 서비스도 같은 보드 사용(스폰 시작 전 1회)
        }
        foreach(var wave in waveTable.GetWave(5))
        {
            SpawnWaveRout(wave).Forget();
        }
        
    }
    public void SpawnWave(int currentStage)
    {
        foreach(var wave in waveTable.GetWave(currentStage))
        {
            SpawnWaveRout(wave).Forget();
        }
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
            var go = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            if (go.TryGetComponent(out EnemyBase enemy))
                enemy.EnterMap(board, waypoints); // 보드 주입 + 스폰→본진 이동 시작

            if (i < wave.Count - 1 && wave.Delay > 0f)
            await UniTask.Delay(TimeSpan.FromSeconds(wave.Delay));
        }
    }
}
