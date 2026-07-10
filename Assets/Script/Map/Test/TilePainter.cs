using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타일 색칠(검증/디버그 시각화 전용). Core(MapBoard/Tile)는 색을 모른다.
/// 나중에 통째로 제거 대상 — 이 컴포넌트만 지우면 게임 로직은 그대로 남는다.
/// 좌표→타일 조회는 board에 위임하고, tint(MaterialPropertyBlock)는 여기서 직접 처리한다.
/// </summary>
public class TilePainter : MonoBehaviour
{
    public MapBoard board; // 주입(자동탐색 금지)

    [Header("Colors")]
    public Color pathColor = new(0.22f, 0.85f, 0.54f);
    public Color okColor = new(0.21f, 0.77f, 0.41f);
    public Color denyColor = new(0.85f, 0.29f, 0.27f);
    [Tooltip("배치 프리뷰/호버 시 유닛 사거리 타일 색.")]
    public Color rangeColor = new(0.30f, 0.60f, 1f);
    [Tooltip("적이 올라온 타일 색.")]
    public Color enemyColor = new(0.90f, 0.25f, 0.20f);
    [Tooltip("아군+적이 겹친(저지 중) 타일 색.")]
    public Color blockColor = new(0.95f, 0.55f, 0.10f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private MaterialPropertyBlock _mpb;
    private readonly Dictionary<Tile, Renderer[]> _rends = new();

    public void SetColor(Vector2Int coord, Color color)
    {
        if (board != null && board.TryGetCell(coord, out Tile tile)) Paint(tile, color);
    }

    public void ClearColor(Vector2Int coord)
    {
        if (board != null && board.TryGetCell(coord, out Tile tile)) Restore(tile);
    }

    private void Paint(Tile tile, Color color)
    {
        _mpb ??= new MaterialPropertyBlock();
        foreach (Renderer r in Renderers(tile))
        {
            if (r == null) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, color);
            r.SetPropertyBlock(_mpb);
        }
    }

    private void Restore(Tile tile)
    {
        _mpb ??= new MaterialPropertyBlock();
        foreach (Renderer r in Renderers(tile))
        {
            if (r == null) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.Clear();
            r.SetPropertyBlock(_mpb);
        }
    }

    private Renderer[] Renderers(Tile tile)
    {
        if (!_rends.TryGetValue(tile, out Renderer[] rends))
        {
            rends = tile.GetComponentsInChildren<Renderer>();
            _rends[tile] = rends;
        }
        return rends;
    }

    // ---- 에디터 디버그: 지형 한눈에 보기(우클릭 실행) ----
    // 노랑=스폰, 파랑=본진(Core), 빨강=High(벽), 초록=Ground(통행), 회색=Empty(벽).

    [ContextMenu("Tint Terrain")]
    public void TintTerrain()
    {
        if (board == null) return;
        if (board.Cells.Count == 0) board.Build();

        foreach (Tile t in board.Cells.Values)
        {
            Color c = t.isEnemySpawn ? new Color(0.95f, 0.9f, 0.2f)
                : t.Terrain switch
                {
                    TerrainType.Core   => new Color(0.2f, 0.5f, 1f),
                    TerrainType.High   => new Color(0.9f, 0.3f, 0.2f),
                    TerrainType.Ground => new Color(0.3f, 0.8f, 0.4f),
                    _                  => new Color(0.5f, 0.5f, 0.5f),
                };
            Paint(t, c);
        }
    }

    [ContextMenu("Clear Tint")]
    public void ClearTint()
    {
        if (board == null) return;
        foreach (Tile t in board.Cells.Values) Restore(t);
    }
}
