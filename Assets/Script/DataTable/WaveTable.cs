using System.Collections.Generic;
using UnityEngine;

public class WaveTable : DataTable
{
    public class Data
    {
        public int ID { get; set; }
        public string MonsterName { get; set; }
        public string Prefab { get; set; }
        public int Count { get; set; }
        public float SpawnTime { get; set; }
        public float Delay { get; set; }
    }

    private readonly List<Data> waves = new();

    public override void Load(string filename)
    {
        waves.Clear();

        var path = $"DataTable/{filename}";
        TextAsset textAsset = Resources.Load<TextAsset>(path);
        if (textAsset == null)
        {
            Debug.LogWarning($"WaveTable: '{path}' 로드 실패");
            return;
        }
        var list = LoadCsv<Data>(textAsset.text);
        foreach (var data in list)
        {
            if (string.IsNullOrEmpty(data.MonsterName)) continue;
            waves.Add(data);
        }
    }

    public IReadOnlyList<Data> GetAll()
    {
        return waves;
    }

    // 같은 ID(웨이브 번호)에 속한 모든 몬스터 행
    public List<Data> GetWave(int id)
    {
        return waves.FindAll(w => w.ID == id);
    }

    public GameObject GetMonsterPrefab(Data data)
    {
        return data == null ? null : LoadMonsterPrefab(data.Prefab);
    }

    private static GameObject LoadMonsterPrefab(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        const string prefix = "Resources/";
        if (key.StartsWith(prefix)) key = key.Substring(prefix.Length);
        return Resources.Load<GameObject>(key);
    }
}
