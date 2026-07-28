using System.Collections.Generic;
using UnityEngine;

public class TilePainter : MonoBehaviour
{
    //디버그용 타일 색상 변경
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
        if (board.TryGetCell(coord, out Tile tile)) Paint(tile, color);
    }

    public void ClearColor(Vector2Int coord)
    {
        if (board.TryGetCell(coord, out Tile tile)) Restore(tile);
    }

    // 타일을 직접 받는 경로 — 좌표는 모듈 로컬이라 보드 역조회가 모듈을 특정 못 하므로,
    // 어느 모듈 타일이든 색칠하려면 이쪽을 쓴다.
    public void SetColor(Tile tile, Color color)
    {
        Paint(tile, color);
    }

    public void ClearColor(Tile tile)
    {
        Restore(tile);
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
        foreach (Renderer r in Renderers(tile))
        {
            if (r == null)
            {
                continue;
            }

            r.SetPropertyBlock(null);
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
        foreach (Tile t in board.Cells.Values) Restore(t);
    }
}
