using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 각 모듈(Grid) 하위 타일을 읽어 2D 미니맵으로 그리고, 스폰→본진 A* 경로를 즉석 미리보기하는
/// 에디터 전용 창. 읽기 전용 — 씬/타일 상태를 전혀 변경하지 않는다(경로도 board.Build 없이
/// 툴이 자체 계산해 EnemyLane 등 공유 상태를 건드리지 않는다).
/// v0: 지형 + 경로 보기. 이후 지형 칠하기·체크포인트 찍기를 여기 얹을 수 있다.
/// </summary>
public class MapPathPreviewWindow : EditorWindow
{
    private const int Cell = 22;      // 미니맵 한 칸 픽셀
    private const int Pad = 24;       // 좌/상 여백(라벨 자리)

    private Grid[] _grids = System.Array.Empty<Grid>();
    private string[] _moduleNames = System.Array.Empty<string>();
    private int _moduleIndex;
    private Vector2 _scroll;

    private static readonly Color CGround  = new(0.75f, 0.86f, 0.60f);
    private static readonly Color CHigh    = new(0.37f, 0.37f, 0.35f);
    private static readonly Color CSpecial = new(0.79f, 0.78f, 0.74f);
    private static readonly Color CCore    = new(0.94f, 0.62f, 0.15f);
    private static readonly Color CEmpty   = new(0.90f, 0.90f, 0.88f);
    private static readonly Color CPath    = new(0.11f, 0.45f, 0.86f);
    private static readonly Color CSpawn   = new(0.87f, 0.24f, 0.22f);

    [MenuItem("Tools/Map/Path Preview")]
    private static void Open()
    {
        GetWindow<MapPathPreviewWindow>("Map Path Preview").minSize = new Vector2(420, 320);
    }

    private void OnEnable() => Refresh();

    // 씬의 모든 Grid = 모듈 목록(TilePosBaker와 동일 전제: Grid 하나 = 모듈 하나).
    private void Refresh()
    {
        var list = new List<Grid>();
        foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            list.AddRange(root.GetComponentsInChildren<Grid>(true));

        _grids = list.ToArray();
        _moduleNames = _grids.Select(g => g.transform.root.name).ToArray();
        if (_moduleIndex >= _grids.Length) _moduleIndex = 0;
    }

    private void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(70)))
                Refresh();

            if (_grids.Length > 0)
                _moduleIndex = EditorGUILayout.Popup(_moduleIndex, _moduleNames, EditorStyles.toolbarPopup, GUILayout.Width(180));

            GUILayout.FlexibleSpace();
        }

        if (_grids.Length == 0)
        {
            EditorGUILayout.HelpBox("씬에 Grid(모듈)가 없습니다. 새로고침을 눌러 다시 검색하세요.", MessageType.Info);
            return;
        }

        Grid grid = _grids[_moduleIndex];
        if (grid == null) { Refresh(); return; }

        Dictionary<Vector2Int, Tile> cells = CollectCells(grid, out int cols, out int rows);
        if (cells.Count == 0)
        {
            EditorGUILayout.HelpBox("이 모듈에 좌표가 새겨진 Tile이 없습니다. Tools/Map/Bake Tile Positions 먼저 실행하세요.", MessageType.Warning);
            return;
        }

        List<Tile> path = FindPath(cells);
        var pathCoords = new HashSet<Vector2Int>(path == null ? Enumerable.Empty<Vector2Int>() : path.Select(t => t.Coord));

        EditorGUILayout.LabelField(
            $"{_moduleNames[_moduleIndex]}   {cols}×{rows}   셀 {grid.cellSize.x:0.#}   경로 {(path == null ? "없음" : path.Count + "칸")}",
            EditorStyles.miniBoldLabel);

        DrawLegend();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        Rect area = GUILayoutUtility.GetRect(Pad + cols * Cell + 8, Pad + rows * Cell + 8);
        DrawGrid(area, cells, cols, rows, pathCoords);
        EditorGUILayout.EndScrollView();
    }

    // 이 모듈 하위 타일 수집. 한 칸에 여럿이면 가장 높은 타일을 대표로(스택 대응).
    private Dictionary<Vector2Int, Tile> CollectCells(Grid grid, out int cols, out int rows)
    {
        var cells = new Dictionary<Vector2Int, Tile>();
        int maxC = 0, maxR = 0;
        foreach (Tile t in grid.GetComponentsInChildren<Tile>(true))
        {
            if (t.State == null) continue;
            Vector2Int c = t.Coord;
            if (!cells.TryGetValue(c, out Tile prev) || t.WorldTop.y > prev.WorldTop.y)
                cells[c] = t;
            if (c.x > maxC) maxC = c.x;
            if (c.y > maxR) maxR = c.y;
        }
        cols = maxC + 1;
        rows = maxR + 1;
        return cells;
    }

    // 순수 A*(BFS): 스폰→본진, 통행 가능 이웃만. 공유 상태를 건드리지 않는다.
    private List<Tile> FindPath(Dictionary<Vector2Int, Tile> cells)
    {
        List<Tile> spawns = cells.Values.Where(t => t.IsEnemySpawn).ToList();
        if (spawns.Count == 0 || !cells.Values.Any(t => t.IsCore)) return null;

        IEnumerable<Tile> Neighbors(Tile t)
        {
            foreach (Vector2Int d in GridCalculator.Directions)
                if (cells.TryGetValue(t.Coord + d, out Tile nb) && nb.Walkable)
                    yield return nb;
        }

        return Pathfinder.FindPath(spawns, t => t.IsCore, Neighbors);
    }

    private void DrawGrid(Rect area, Dictionary<Vector2Int, Tile> cells, int cols, int rows, HashSet<Vector2Int> pathCoords)
    {
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                var coord = new Vector2Int(c, r);
                float x = area.x + Pad + c * Cell;
                float y = area.y + Pad + (rows - 1 - r) * Cell; // row 0 = 아래
                var rect = new Rect(x, y, Cell - 1, Cell - 1);

                if (!cells.TryGetValue(coord, out Tile tile))
                {
                    continue; // 빈 칸은 안 그림
                }

                EditorGUI.DrawRect(rect, TerrainColor(tile));

                if (tile.IsEnemySpawn)
                    EditorGUI.DrawRect(Inset(rect, 5), CSpawn);
                else if (!tile.IsCore && pathCoords.Contains(coord))
                    EditorGUI.DrawRect(Inset(rect, 6), CPath);
            }

        // 좌표 라벨(짝수만) — 방향 감 잡기용
        var lab = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
        for (int c = 0; c < cols; c += 2)
            GUI.Label(new Rect(area.x + Pad + c * Cell, area.y, Cell, Pad), c.ToString(), lab);
        for (int r = 0; r < rows; r += 2)
            GUI.Label(new Rect(area.x, area.y + Pad + (rows - 1 - r) * Cell, Pad, Cell), r.ToString(), lab);
    }

    private static Color TerrainColor(Tile t)
    {
        switch (t.Terrain)
        {
            case TerrainType.Ground: return CGround;
            case TerrainType.High:   return CHigh;
            case TerrainType.Core:   return CCore;
            case TerrainType.Special:return CSpecial;
            default:                 return CEmpty;
        }
    }

    private static Rect Inset(Rect r, float m) => new(r.x + m, r.y + m, r.width - 2 * m, r.height - 2 * m);

    private void DrawLegend()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            Swatch(CGround, "Ground"); Swatch(CHigh, "High(벽)"); Swatch(CSpecial, "Special");
            Swatch(CCore, "Core"); Swatch(CSpawn, "Spawn"); Swatch(CPath, "경로");
            GUILayout.FlexibleSpace();
        }
    }

    private void Swatch(Color c, string label)
    {
        Rect r = GUILayoutUtility.GetRect(14, 14, GUILayout.Width(14), GUILayout.Height(14));
        EditorGUI.DrawRect(r, c);
        GUILayout.Label(label, EditorStyles.miniLabel);
        GUILayout.Space(6);
    }
}
