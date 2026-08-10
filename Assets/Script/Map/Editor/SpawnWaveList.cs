using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 적 탭. 스폰 순서대로 카드를 늘어놓고, 고른 카드의 스탯을 오른쪽에 편다.
/// 지역·라운드는 모듈에 저장된 값이 아니다 — 어느 지역인지는 WaveSpawner 인스펙터에만 있어서
/// 사람이 여기서 직접 골라 훑어보는 값이다.
/// </summary>
public static class SpawnWaveList
{
    private const int Card = 92;
    private const int Thumb = 64;

    /// <summary>탭 전체. 고른 카드를 돌려준다(안 골랐으면 받은 것 그대로).</summary>
    public static SpawnWaveReadout.Entry Draw(
        int region, int round, List<SpawnWaveReadout.Entry> entries, SpawnWaveReadout.Entry chosen)
    {
        if (entries.Count == 0)
        {
            GUILayout.Label($"지역 {region} · {round}라운드에 웨이브 데이터가 없습니다.",
                EditorStyles.wordWrappedMiniLabel);
            return chosen;
        }

        SpawnWaveReadout.Entry picked = ValidChoice(entries, chosen);

        using (new EditorGUILayout.HorizontalScope())
        {
            picked = DrawCards(entries, picked);
            DrawDetail(picked ?? entries[0]);
        }

        return picked;
    }

    // 지역·라운드를 바꿔서 고른 카드가 새 목록에 없으면 고른 상태를 버린다.
    private static SpawnWaveReadout.Entry ValidChoice(List<SpawnWaveReadout.Entry> entries, SpawnWaveReadout.Entry chosen)
    {
        if (chosen == null || entries.Contains(chosen))
        {
            return chosen;
        }

        return null;
    }

    private static SpawnWaveReadout.Entry DrawCards(List<SpawnWaveReadout.Entry> entries, SpawnWaveReadout.Entry chosen)
    {
        SpawnWaveReadout.Entry picked = chosen;

        using (new EditorGUILayout.VerticalScope(GUILayout.Width(Card + 8)))
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (DrawCard(entries[i], entries[i] == chosen))
                {
                    picked = entries[i];
                }
            }
        }

        return picked;
    }

    private static bool DrawCard(SpawnWaveReadout.Entry entry, bool chosen)
    {
        Rect slot = GUILayoutUtility.GetRect(Card, Thumb + 34, GUILayout.Width(Card), GUILayout.Height(Thumb + 34));

        var frame = new Rect(slot.x, slot.y, slot.width, Thumb);
        if (chosen)
        {
            EditorGUI.DrawRect(frame, MapMakerPalette.Mark);
        }

        var inner = new Rect(frame.x + 2f, frame.y + 2f, frame.width - 4f, frame.height - 4f);
        EditorGUI.DrawRect(inner, MapMakerPalette.Panel);
        DrawThumb(inner, entry);

        var timeLine = new Rect(slot.x, frame.yMax, slot.width, 15f);
        GUI.Label(timeLine, TimeWord(entry), EditorStyles.miniLabel);

        var nameLine = new Rect(slot.x, timeLine.yMax, slot.width, 15f);
        GUI.Label(nameLine, $"{entry.Wave.MonsterName} ×{entry.Count}", EditorStyles.miniBoldLabel);

        return GUI.Button(slot, GUIContent.none, GUIStyle.none);
    }

    private static void DrawThumb(Rect inner, SpawnWaveReadout.Entry entry)
    {
        GameObject prefab = DataTableManager.WaveTable.GetMonsterPrefab(entry.Wave);
        if (prefab == null)
        {
            return;
        }

        Texture2D shot = AssetPreview.GetAssetPreview(prefab);
        if (shot != null)
        {
            GUI.DrawTexture(inner, shot, ScaleMode.ScaleToFit);
        }
    }

    private static string TimeWord(SpawnWaveReadout.Entry entry)
    {
        if (entry.Wave.SpawnTime <= 0f)
        {
            return "0초";
        }

        return $"{entry.Wave.SpawnTime}초 뒤";
    }

    private static void DrawDetail(SpawnWaveReadout.Entry entry)
    {
        using (new EditorGUILayout.VerticalScope())
        {
            if (entry.Enemy == null)
            {
                GUILayout.Label($"{entry.Wave.MonsterName} — EnemyTable에 정보 없음", EditorStyles.boldLabel);
                return;
            }

            DrawStats(entry.Enemy);
        }
    }

    private static void DrawStats(EnemyTable.Data data)
    {
        GUILayout.Label($"{data.Name}  ({data.Class})", EditorStyles.boldLabel);
        GUILayout.Label($"체력 {data.Health} · 공격 {data.Attack} · 방어 {data.Defense}", EditorStyles.miniLabel);
        GUILayout.Label($"이속 {data.MoveSpeed} · 사거리 {data.Range} · {data.Type}", EditorStyles.miniLabel);
        DrawIfSet("속성", data.Attribute, "None");
        DrawIfSet("스킬", data.Skills, string.Empty);
    }

    private static void DrawIfSet(string label, string value, string blank)
    {
        if (string.IsNullOrEmpty(value) || value == blank)
        {
            return;
        }

        GUILayout.Label($"{label} {value}", EditorStyles.miniLabel);
    }
}
