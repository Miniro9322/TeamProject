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

    private readonly Dictionary<int, Data> table = new();

    public override void Load(string filename)
    {
        table.Clear();

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
            if (!table.ContainsKey(data.ID)) table.Add(data.ID, data);
            else Debug.LogWarning($"WaveTable 키 중복 '{data.ID}'");
        }
    }

    public Data Get(int id)
    {
        return table.TryGetValue(id, out var data) ? data : null;
    }

    public IReadOnlyCollection<Data> GetAll()
    {
        return table.Values;
    }

    public GameObject GetMonsterPrefab(int id)
    {
        var data = Get(id);
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
