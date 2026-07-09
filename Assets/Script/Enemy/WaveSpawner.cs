using System.Collections;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    // 소환 테스트용 나중에 지우거나 변경 ( 포탈 만들시 해당 포탈 어디에서 소환할지 추가예정)
    [SerializeField] private Vector3 spawnPosition = Vector3.zero;

    private WaveTable waveTable;

    private void Start()
    {
        waveTable = DataTableManager.Get<WaveTable>(DataTableIds.Wave);
        if (waveTable == null)
        {
            Debug.LogWarning("WaveSpawner: WaveTable을 찾을 수 없음");
            return;
        }

        foreach (var wave in waveTable.GetAll())
        {
            StartCoroutine(SpawnWave(wave));
        }
    }

    private IEnumerator SpawnWave(WaveTable.Data wave)
    {
        var prefab = waveTable.GetMonsterPrefab(wave);
        if (prefab == null)
        {
            Debug.LogWarning($"WaveSpawner: 프리팹 로드 실패 '{wave.Prefab}' (ID {wave.ID})");
            yield break;
        }

        if (wave.SpawnTime > 0f) yield return new WaitForSeconds(wave.SpawnTime);

        for (int i = 0; i < wave.Count; i++)
        {
            Instantiate(prefab, spawnPosition, Quaternion.identity);
 
            if (i < wave.Count - 1 && wave.Delay > 0f)
                yield return new WaitForSeconds(wave.Delay);
        }
    }
}
