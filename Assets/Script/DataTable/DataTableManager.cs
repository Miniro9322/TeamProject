using System.Collections.Generic;
using UnityEngine;

public static class DataTableManager
{
    private static readonly Dictionary<string, DataTable> tables = new Dictionary<string, DataTable>();
    public static StringTable StringTable => Get<StringTable>(DataTableIds.String);
    public static EnemyTable EnemyTable => Get<EnemyTable>(DataTableIds.Enemy);
    public static WaveTable WaveTable => Get<WaveTable>(DataTableIds.Wave);
    public static SkillTable SkillTable => Get<SkillTable>(DataTableIds.Skill);
    public static PortalTable PortalTable => Get<PortalTable>(DataTableIds.Portal);
    static DataTableManager()
    {
        Init();
    }
    private static void Init()
    {
        var stringTable = new StringTable();
        stringTable.Load(DataTableIds.String);
        tables.Add(DataTableIds.String, stringTable);
        var enemyTable = new EnemyTable();
        enemyTable.Load(DataTableIds.Enemy);
        tables.Add(DataTableIds.Enemy,enemyTable);
        var waveTable = new WaveTable();
        waveTable.Load(DataTableIds.Wave);
        tables.Add(DataTableIds.Wave, waveTable);
        var skillTable = new SkillTable();
        skillTable.Load(DataTableIds.Skill);
        tables.Add(DataTableIds.Skill, skillTable);
        var portalTable = new PortalTable();
        portalTable.Load(DataTableIds.Portal);
        tables.Add(DataTableIds.Portal, portalTable);
    }
    public static T Get<T>(string id) where T : DataTable
    {
        if (!tables.ContainsKey(id))
        {
            Debug.Log($"테이블 없음: {id}");
            return null;
        }
        return tables[id] as T;
    }
}
