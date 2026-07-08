using System.Collections.Generic;
using UnityEngine;

public class StringTable : DataTable
{
    public static readonly string UnKnown = "키 없음";

    public class Data
    {
        public string ID { get; set; }
        public string Kr { get; set; }
        public string En { get; set; }
        public string Jp { get; set; }
    }
    public static Language CurrentLanguage = Language.Kr;
    private readonly Dictionary<string, Data> table = new Dictionary<string, Data>();
    public override void Load(string filename)
    {
        table.Clear();

        var path = $"DataTable/{filename}";
        TextAsset textAsset = Resources.Load<TextAsset>(path);
        var list = LoadCsv<Data>(textAsset.text);
        foreach (var data in list)
        {
            if (string.IsNullOrEmpty(data.ID)) continue;
            if (!table.ContainsKey(data.ID))
            {
                table.Add(data.ID, data);
            }
            else
            {
                Debug.LogWarning($"키 중복'{data.ID} - {filename}'");
            }
        }
    }

    public string Get(string key)
    {
        if (!table.ContainsKey(key))
        {
            return UnKnown;
        }
        var data = table[key];
        return CurrentLanguage switch
        {
            Language.Kr => data.Kr,
            Language.En => data.En,
            Language.jp => data.Jp,
            _ => UnKnown
        };
    }
}
