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
    public void Flash(Vector3 centerWorld, int range, RangeShape shape)
    {
        if (board == null || painter == null) return;

        Vector2Int center = board.WorldToCell(centerWorld);
        foreach (Tile tile in TileShapeQuery.GetTiles(board, center, range, shape))
            PaintFlash(tile.Coord);
    }

    // originWorld에서 direction 방향으로 length칸의 직선 판정 타일을 강조 표시.
    public void FlashLine(Vector3 originWorld, Vector2Int direction, int length)
    {
        if (board == null || painter == null) return;

        Vector2Int origin = board.WorldToCell(originWorld);
        foreach (Tile tile in TileShapeQuery.GetLineTiles(board, origin, direction, length))
            PaintFlash(tile.Coord);
    }

    // 체인 라이트닝이 맞춘 적들의 타일을 순서대로 강조 표시.
    public void FlashChain(IEnumerable<GameObject> hitOrder)
    {
        if (board == null || painter == null || hitOrder == null) return;

        foreach (GameObject go in hitOrder)
        {
            if (go == null) continue;
            PaintFlash(board.WorldToCell(go.transform.position));
        }
    }

    private void PaintFlash(Vector2Int coord)
    {
        Color splashColor = new(1f, 0.75f, 0.2f);
        painter.SetColor(coord, splashColor);
        _release[coord] = Time.time + duration; // 겹치면 가장 늦은 복원 시각으로 연장
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
