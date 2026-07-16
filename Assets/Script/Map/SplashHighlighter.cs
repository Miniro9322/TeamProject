using System.Collections.Generic;
using UnityEngine;

// 범위 공격(Splash) 판정 타일을 착탄 순간 잠깐 강조 표시하는 씬 공용 서비스.
public class SplashHighlighter : MonoBehaviour
{
    public static SplashHighlighter Instance { get; private set; }

    [SerializeField] private MapBoard board;
    [SerializeField] private TilePainter painter;
    [SerializeField] private float duration = 0.3f;

    private readonly Dictionary<Vector2Int, float> _release = new(); // coord -> 복원 시각
    private readonly List<Vector2Int> _expired = new();

    private void Awake() => Instance = this;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // centerWorld 기준 판정 타일을 splashColor로 칠하고 duration 뒤 자동 복원.
    public void Flash(Vector3 centerWorld, int range, bool square)
    {
        if (board == null || painter == null) return;

        Vector2Int center = board.WorldToCell(centerWorld);
        float until = Time.time + duration;

        foreach (Tile tile in board.GetTiles(center, range, square))
        {
            Color splashColor = new(1f, 0.75f, 0.2f);
            painter.SetColor(tile.Coord, splashColor);
            _release[tile.Coord] = until; // 겹치면 가장 늦은 복원 시각으로 연장
        }
    }

    private void Update()
    {
        if (_release.Count == 0) return;

        float now = Time.time;
        _expired.Clear();
        foreach (var kv in _release)
        {
            if (now >= kv.Value) _expired.Add(kv.Key);
        }

        foreach (Vector2Int coord in _expired)
        {
            painter.ClearColor(coord);
            _release.Remove(coord);
        }
    }
}
