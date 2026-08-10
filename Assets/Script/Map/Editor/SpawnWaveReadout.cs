using System.Collections.Generic;

/// <summary>
/// 한 지역·라운드에 나오는 적을 스폰 순서대로 모은다. 표시 전용 — 아무것도 쓰지 않는다.
/// </summary>
public static class SpawnWaveReadout
{
    /// <summary>웨이브 한 줄(적 한 종류)과 화면에 쓸 값들.</summary>
    public class Entry
    {
        public WaveTable.Data Wave;
        public EnemyTable.Data Enemy;
        public int Count;
    }

    /// <summary>이 지역·라운드에 나오는 적들. 등장 순서(SpawnTime)로 정렬해 돌려준다.</summary>
    public static List<Entry> Collect(int region, int stage)
    {
        var entries = new List<Entry>();
        WaveTable waveTable = DataTableManager.WaveTable;
        if (waveTable == null)
        {
            return entries;
        }

        int lookupId = WaveSpawner.GetStageLookupId(stage);
        List<WaveTable.Data> waves = waveTable.GetWave(region, lookupId);

        for (int i = 0; i < waves.Count; i++)
        {
            entries.Add(ToEntry(waves[i], stage));
        }

        entries.Sort(BySpawnTime);
        return entries;
    }

    private static Entry ToEntry(WaveTable.Data wave, int stage)
    {
        return new Entry
        {
            Wave = wave,
            Enemy = DataTableManager.EnemyTable?.Get(wave.MonsterName),
            Count = WaveSpawner.GetScaleCount(wave.Count, stage)
        };
    }

    private static int BySpawnTime(Entry a, Entry b)
    {
        return a.Wave.SpawnTime.CompareTo(b.Wave.SpawnTime);
    }
}
