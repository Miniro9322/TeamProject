using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// 빌드본에서 플레이 데이터를 수집하는 진입점. 게임플레이 코드는 아래 static 메서드만 호출하면 되고,
// 버퍼링/로컬 파일 기록/원격 전송은 전부 이 안에서 처리한다 — DI 등록 불필요(PoolManager.Instance 같은
// 기존 싱글톤 폴백 패턴과 동일하게 RuntimeInitializeOnLoadMethod로 자동 부트스트랩).
public static class AnalyticsRecorder
{
    [Serializable]
    private class HeroDamageRecord
    {
        public string evt = "hero_damage";
        public string session;
        public string wallClockUtc;
        public string heroName;
        public int heroUnitId;
        public string enemyType;
        public string enemyClass;
        public int region;
        public int damage;
        public bool isCrit;
        public bool isKill;
    }

    [Serializable]
    private class EnemyLeakedRecord
    {
        public string evt = "enemy_leaked";
        public string session;
        public string wallClockUtc;
        public string enemyType;
        public string enemyClass;
        public int region;
        public int dayCount;
        public int hpLost;
        public int hpRemaining;
    }

    [Serializable]
    private class RoundStartRecord
    {
        public string evt = "round_start";
        public string session;
        public string wallClockUtc;
        public int region;
        public int dayCount;
        public int enemyCount;
    }

    [Serializable]
    private class RoundEndRecord
    {
        public string evt = "round_end";
        public string session;
        public string wallClockUtc;
        public int region;
        public int dayCount;
        public float durationSeconds;
        public int hpLostThisRound;
    }

    [Serializable]
    private class HeroPlacedRecord
    {
        public string evt = "hero_placed";
        public string session;
        public string wallClockUtc;
        public string heroName;
        public int heroUnitId;
        public int region;
        public int tileX;
        public int tileY;
    }

    [Serializable]
    private class GameOverRecord
    {
        public string evt = "game_over";
        public string session;
        public string wallClockUtc;
        public int dayCount;
        public int finalHp;
    }

    private static readonly string SessionId = Guid.NewGuid().ToString("N");
    // 지역별 "이번 라운드 시작 이후 깎인 체력" 누적 — RoundStart가 리셋, EnemyLeaked가 누적, RoundEnd가 읽는다.
    private static readonly Dictionary<int, int> HpLostSinceRoundStart = new();

    private static readonly List<string> Buffer = new();
    private static Runner runner;
    private static AnalyticsSettings settings;
    private static string FilePath;

    private static void EnsureInit()
    {
        if (runner != null) return;

        settings = Resources.Load<AnalyticsSettings>("AnalyticsSettings");

        string dir = Path.Combine(Application.persistentDataPath, "Analytics");
        try { Directory.CreateDirectory(dir); }
        catch (Exception e) { Debug.LogWarning($"AnalyticsRecorder: 로그 폴더 생성 실패 — {e.Message}"); }
        FilePath = Path.Combine(dir, $"session_{SessionId}.jsonl");

        var go = new GameObject("AnalyticsRecorder");
        UnityEngine.Object.DontDestroyOnLoad(go);
        runner = go.AddComponent<Runner>();
    }

    public static void RoundStart(int region, int dayCount, int enemyCount)
    {
        EnsureInit();
        HpLostSinceRoundStart[region] = 0;
        Enqueue(new RoundStartRecord
        {
            session = SessionId,
            wallClockUtc = NowIso(),
            region = region,
            dayCount = dayCount,
            enemyCount = enemyCount,
        });
    }

    public static void RoundEnd(int region, int dayCount, float durationSeconds)
    {
        EnsureInit();
        HpLostSinceRoundStart.TryGetValue(region, out int hpLost);
        Enqueue(new RoundEndRecord
        {
            session = SessionId,
            wallClockUtc = NowIso(),
            region = region,
            dayCount = dayCount,
            durationSeconds = durationSeconds,
            hpLostThisRound = hpLost,
        });
    }

    public static void HeroDamage(string heroName, int heroUnitId, string enemyType, string enemyClass,
        int region, int damage, bool isCrit, bool isKill)
    {
        EnsureInit();
        Enqueue(new HeroDamageRecord
        {
            session = SessionId,
            wallClockUtc = NowIso(),
            heroName = heroName,
            heroUnitId = heroUnitId,
            enemyType = enemyType,
            enemyClass = enemyClass,
            region = region,
            damage = damage,
            isCrit = isCrit,
            isKill = isKill,
        });
    }

    public static void EnemyLeaked(string enemyType, string enemyClass, int region, int dayCount, int hpLost, int hpRemaining)
    {
        EnsureInit();
        if (HpLostSinceRoundStart.TryGetValue(region, out int acc))
            HpLostSinceRoundStart[region] = acc + hpLost;
        else
            HpLostSinceRoundStart[region] = hpLost;

        Enqueue(new EnemyLeakedRecord
        {
            session = SessionId,
            wallClockUtc = NowIso(),
            enemyType = enemyType,
            enemyClass = enemyClass,
            region = region,
            dayCount = dayCount,
            hpLost = hpLost,
            hpRemaining = hpRemaining,
        });
    }

    public static void HeroPlaced(string heroName, int heroUnitId, int region, int tileX, int tileY)
    {
        EnsureInit();
        Enqueue(new HeroPlacedRecord
        {
            session = SessionId,
            wallClockUtc = NowIso(),
            heroName = heroName,
            heroUnitId = heroUnitId,
            region = region,
            tileX = tileX,
            tileY = tileY,
        });
    }

    public static void GameOver(int dayCount, int finalHp)
    {
        EnsureInit();
        Enqueue(new GameOverRecord
        {
            session = SessionId,
            wallClockUtc = NowIso(),
            dayCount = dayCount,
            finalHp = finalHp,
        });
        runner.FlushNow(); // 게임오버 직후 씬 전환/종료로 유실되지 않게 즉시 내보낸다
    }

    private static string NowIso() => DateTime.UtcNow.ToString("o");

    private static void Enqueue(object record)
    {
        Buffer.Add(JsonUtility.ToJson(record));
        int max = settings != null ? settings.maxBufferedEvents : 50;
        if (Buffer.Count >= max) runner.FlushNow();
    }

    // 실제 파일 쓰기/네트워크 전송/주기적 flush를 맡는 hidden MonoBehaviour.
    private class Runner : MonoBehaviour
    {
        private float timer;

        private void Update()
        {
            float interval = settings != null ? settings.flushIntervalSeconds : 15f;
            timer += Time.unscaledDeltaTime;
            if (timer >= interval)
            {
                timer = 0f;
                FlushNow();
            }
        }

        public void FlushNow()
        {
            if (Buffer.Count == 0) return;

            var batch = new List<string>(Buffer);
            Buffer.Clear();
            WriteLocal(batch);
            SendRemote(batch);
        }

        private void WriteLocal(List<string> batch)
        {
            try
            {
                var sb = new StringBuilder();
                foreach (string line in batch) sb.Append(line).Append('\n');
                File.AppendAllText(FilePath, sb.ToString());
            }
            catch (Exception e)
            {
                Debug.LogWarning($"AnalyticsRecorder: 로컬 파일 기록 실패 — {e.Message}");
            }
        }

        private void SendRemote(List<string> batch)
        {
            if (settings == null || string.IsNullOrEmpty(settings.remoteEndpointUrl)) return;
            StartCoroutine(PostBatch(settings.remoteEndpointUrl, batch));
        }

        private System.Collections.IEnumerator PostBatch(string url, List<string> batch)
        {
            var sb = new StringBuilder("[");
            for (int i = 0; i < batch.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(batch[i]);
            }
            sb.Append(']');

            using UnityWebRequest req = new(url, UnityWebRequest.kHttpVerbPOST);
            byte[] body = Encoding.UTF8.GetBytes(sb.ToString());
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
                Debug.LogWarning($"AnalyticsRecorder: 원격 전송 실패({req.error}) — 로컬 파일에는 남아있음.");
        }

        private void OnApplicationQuit() => FlushNow();
        private void OnApplicationPause(bool paused) { if (paused) FlushNow(); }
    }
}
