using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 모듈의 타일 격자를 2D로 그린다.
///
/// 한 칸에 두 층의 정보를 겹쳐 보여준다 — 바탕색은 지형(큰 분류), 칸 안의 점은 배치 허용(작은 분류).
/// 지금 고른 붓과 상관없는 칸은 배경 쪽으로 죽여, 무슨 모드로 보고 있는지가 격자 자체에 드러나게 한다.
///
/// row 0을 아래에 놓는다. 씬을 위에서 내려다본 그림과 위아래가 같아야 좌표를 옮겨 짚을 수 있다.
/// </summary>
public class TileGridView
{
    /// <summary>좌·상단 여백 — 좌표 라벨 자리.</summary>
    public const int Pad = 22;

    private readonly Dictionary<Vector2Int, Tile> _cells;
    private readonly MapBrush _brush;

    public int Cols { get; }
    public int Rows { get; }

    public TileGridView(Dictionary<Vector2Int, Tile> cells, MapBrush brush)
    {
        _cells = cells;
        _brush = brush;

        int maxCol = 0;
        int maxRow = 0;
        foreach (Vector2Int coord in cells.Keys)
        {
            if (coord.x > maxCol)
            {
                maxCol = coord.x;
            }

            if (coord.y > maxRow)
            {
                maxRow = coord.y;
            }
        }

        Cols = maxCol + 1;
        Rows = maxRow + 1;
    }

    public float PixelWidth(int cellPixels)
    {
        return Pad + Cols * cellPixels + 6;
    }

    public float PixelHeight(int cellPixels)
    {
        return Pad + Rows * cellPixels + 6;
    }

    /// <summary>격자와 경로를 그린다. path는 스폰→본진 순서로 정렬돼 있어야 한다.</summary>
    public void Draw(Rect area, int cellPixels, List<Tile> path, Vector2Int hover)
    {
        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Cols; col++)
            {
                var coord = new Vector2Int(col, row);
                bool exists = _cells.TryGetValue(coord, out Tile tile);
                if (!exists)
                {
                    continue; // 타일이 아예 없는 칸은 그리지 않는다(Empty로 칠한 벽과 구분된다)
                }

                DrawCell(CellRect(area, cellPixels, col, row), tile, cellPixels);
            }
        }

        DrawPath(area, cellPixels, path);
        DrawHover(area, cellPixels, hover);
        DrawAxisLabels(area, cellPixels);
    }

    /// <summary>마우스 위치가 가리키는 칸. 격자 밖이면 (-1,-1).</summary>
    public Vector2Int CoordAt(Vector2 mouse, Rect area, int cellPixels)
    {
        float localX = mouse.x - (area.x + Pad);
        float localY = mouse.y - (area.y + Pad);

        int col = Mathf.FloorToInt(localX / cellPixels);
        int rowFromTop = Mathf.FloorToInt(localY / cellPixels);
        int row = Rows - 1 - rowFromTop;

        var coord = new Vector2Int(col, row);
        if (!GridCalculator.IsInGrid(coord, Cols, Rows))
        {
            return new Vector2Int(-1, -1);
        }

        return coord;
    }

    // ---- 칸 ----

    private void DrawCell(Rect rect, Tile tile, int cellPixels)
    {
        bool lit = Lit(tile);

        Color fill = MapMakerPalette.Terrain(tile.Terrain);
        Color top = MapMakerPalette.HighTop;
        Color mark = MapMakerPalette.CoreMark;
        if (!lit)
        {
            fill = MapMakerPalette.Dim(fill);
            top = MapMakerPalette.Dim(top);
            mark = MapMakerPalette.Dim(mark);
        }

        EditorGUI.DrawRect(rect, fill);

        // 고지는 한 단 올라와 있다 — 윗면에 밝은 띠를 얹어 지상과 실루엣부터 다르게 만든다.
        if (tile.Terrain == TerrainType.High)
        {
            float band = Mathf.Max(2f, cellPixels * 0.22f);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, band), top);
        }

        if (tile.Terrain == TerrainType.Core)
        {
            EditorGUI.DrawRect(Inset(rect, cellPixels * 0.28f), mark);
        }

        if (tile.IsEnemySpawn)
        {
            DrawBorder(rect, MapMakerPalette.Spawn, 2f);
        }

        // 지금 고른 배치 허용이 켜진 칸에만 점을 찍는다 — 모드마다 다른 층을 보는 셈이다.
        if (lit && TileFlagQuery.IsOn(tile, _brush))
        {
            float size = Mathf.Max(4f, cellPixels * 0.26f);
            float x = rect.x + (rect.width - size) * 0.5f;
            float y = rect.y + rect.height - size - 2f;
            DrawFlagMark(new Rect(x, y, size, size), tile);
        }

        if (TileAuthorRule.IsDeadCell(tile))
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 3f, rect.height), MapMakerPalette.Problem);
        }
    }

    /// <summary>
    /// 켜진 허용 표식. 지금 규칙에서 효력이 있으면 꽉 찬 네모, 없으면 속 빈 네모다.
    /// 고지에 근접을 찍는 것 같은 조작은 잘못이 아니라 나중을 위한 대비일 수 있으므로 막지 않는다 —
    /// 다만 지금은 아무 일도 일어나지 않는다는 것이 찍은 자리에서 바로 보여야 한다.
    /// </summary>
    private void DrawFlagMark(Rect dot, Tile tile)
    {
        if (TileFlagQuery.TakesEffect(tile.Terrain, _brush))
        {
            EditorGUI.DrawRect(dot, MapMakerPalette.Mark);
            return;
        }

        DrawBorder(dot, MapMakerPalette.Mark, 1f);
    }

    /// <summary>이 칸이 지금 고른 붓의 대상인가. None이면 전부 밝게 둔다(읽기 전용).</summary>
    private bool Lit(Tile tile)
    {
        switch (_brush)
        {
            case MapBrush.None: return true;
            case MapBrush.Ground: return tile.Terrain == TerrainType.Ground;
            case MapBrush.High: return tile.Terrain == TerrainType.High;
            case MapBrush.Core: return tile.Terrain == TerrainType.Core;
            case MapBrush.Special: return tile.Terrain == TerrainType.Special;
            case MapBrush.Empty: return tile.Terrain == TerrainType.Empty;
            case MapBrush.Spawn: return tile.IsEnemySpawn;
            default: return TileFlagQuery.IsOn(tile, _brush);
        }
    }

    // ---- 경로 ----

    /// <summary>
    /// 경로를 칸 채우기가 아니라 선으로 긋는다. 채우면 그 칸의 지형과 배치 표식이 가려져,
    /// "길이 어디로 나는가"와 "이 칸이 무엇인가"를 동시에 볼 수 없다.
    /// 4방향 경로라 모든 구간이 가로 또는 세로다 — 사각형 두 장으로 검은 테두리와 흰 선을 만든다.
    /// </summary>
    private void DrawPath(Rect area, int cellPixels, List<Tile> path)
    {
        if (path == null)
        {
            return;
        }

        for (int i = 0; i + 1 < path.Count; i++)
        {
            Vector2 from = CellCenter(area, cellPixels, path[i].Coord);
            Vector2 to = CellCenter(area, cellPixels, path[i + 1].Coord);

            float x = Mathf.Min(from.x, to.x);
            float y = Mathf.Min(from.y, to.y);
            float width = Mathf.Abs(from.x - to.x);
            float height = Mathf.Abs(from.y - to.y);

            EditorGUI.DrawRect(new Rect(x - 2.5f, y - 2.5f, width + 5f, height + 5f), MapMakerPalette.PathEdge);
            EditorGUI.DrawRect(new Rect(x - 1f, y - 1f, width + 2f, height + 2f), MapMakerPalette.Path);
        }
    }

    private void DrawHover(Rect area, int cellPixels, Vector2Int hover)
    {
        if (!GridCalculator.IsInGrid(hover, Cols, Rows))
        {
            return;
        }

        DrawBorder(CellRect(area, cellPixels, hover.x, hover.y), MapMakerPalette.Mark, 1f);
    }

    // ---- 자리 계산 ----

    private Rect CellRect(Rect area, int cellPixels, int col, int row)
    {
        float x = area.x + Pad + col * cellPixels;
        float y = area.y + Pad + (Rows - 1 - row) * cellPixels; // row 0 = 아래
        return new Rect(x, y, cellPixels - 1, cellPixels - 1);
    }

    private Vector2 CellCenter(Rect area, int cellPixels, Vector2Int coord)
    {
        Rect rect = CellRect(area, cellPixels, coord.x, coord.y);
        return rect.center;
    }

    // 좌표 라벨(짝수만) — 방향 감 잡기용
    private void DrawAxisLabels(Rect area, int cellPixels)
    {
        var style = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };

        for (int col = 0; col < Cols; col += 2)
        {
            var slot = new Rect(area.x + Pad + col * cellPixels, area.y, cellPixels, Pad);
            GUI.Label(slot, col.ToString(), style);
        }

        for (int row = 0; row < Rows; row += 2)
        {
            float y = area.y + Pad + (Rows - 1 - row) * cellPixels;
            GUI.Label(new Rect(area.x, y, Pad, cellPixels), row.ToString(), style);
        }
    }

    private static Rect Inset(Rect rect, float margin)
    {
        return new Rect(rect.x + margin, rect.y + margin,
            rect.width - 2f * margin, rect.height - 2f * margin);
    }

    private static void DrawBorder(Rect rect, Color color, float thickness)
    {
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }
}
