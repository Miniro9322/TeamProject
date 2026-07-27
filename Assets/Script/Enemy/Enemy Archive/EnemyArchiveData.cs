using System;
using System.Collections.Generic;
using UnityEngine;

// 도감 해금 데이터. 적이 처음 등장하면 그 enemyKey를 여기에 등록한다.
// (도감 창 열고/닫는 UI는 EnemyArchiveManager가 담당 — 여긴 "누가 등장했나" 상태만 보관)
// PlayerPrefs에 저장되어 다음 실행에도 유지된다.
public static class EnemyArchiveData
{
    private static readonly HashSet<string> unlocked = new();

    public static event Action<string> OnUnlocked;

    private const string PrefsKey = "EnemyArchive.Unlocked";
    private const char Separator = ';';

    static EnemyArchiveData() => Load();

    public static void Unlock(string enemyKey)
    {
        if (string.IsNullOrEmpty(enemyKey)) return;
        if (!unlocked.Add(enemyKey)) return;   // 이미 있으면 false → 조기 반환

        Save();
        OnUnlocked?.Invoke(enemyKey);
    }

    public static bool IsUnlocked(string enemyKey)
        => !string.IsNullOrEmpty(enemyKey) && unlocked.Contains(enemyKey);

    public static IReadOnlyCollection<string> All => unlocked;

    // 디버그/테스트용 — 도감 전체 초기화
    public static void ResetAll()
    {
        unlocked.Clear();
        PlayerPrefs.DeleteKey(PrefsKey);
        PlayerPrefs.Save();
    }

    private static void Save()
    {
        PlayerPrefs.SetString(PrefsKey, string.Join(Separator.ToString(), unlocked));
        PlayerPrefs.Save();
    }

    private static void Load()
    {
        unlocked.Clear();
        var raw = PlayerPrefs.GetString(PrefsKey, string.Empty);
        if (string.IsNullOrEmpty(raw)) return;
        foreach (var k in raw.Split(Separator))
            if (!string.IsNullOrEmpty(k)) unlocked.Add(k);
    }
}
