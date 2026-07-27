using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 맵 메이커 창의 화면 배치와 붓질 입력을 맡는다.
///
/// 유니티 에디터와 같은 짜임이다 — 위는 대상 선택, 왼쪽은 지형(큰 분류), 가운데는 격자,
/// 오른쪽은 배치 허용(작은 분류)과 집계, 아래는 지금 가리키는 칸의 상태.
/// 창이 좁아 셋을 나란히 못 놓으면 오른쪽 판만 격자 아래로 접는다(격자를 자르거나 줄이지 않는다).
///
/// 붓을 고르는 것이 곧 보기 모드를 고르는 것이다 — 그 붓과 상관없는 칸은 격자에서 죽는다.
/// None으로 두면 아무것도 기록하지 않는 읽기 전용이 된다.
///
/// 지형을 칠해도 Col/Row는 변하지 않는다(좌표는 TilePosBaker가 월드 위치에서 뽑는다).
/// 그래서 칠한 뒤 재베이크가 필요 없고, 매 리페인트마다 경로를 다시 계산해도 부담이 없다.
/// EnemyLane은 저작하지 않는다(런타임에 MapBoard.SetLanes가 경로에서 파생시키는 값).
/// </summary>
public class MapMakerWindow : EditorWindow
{
    private const int LeftWidth = 104;
    private const int RightWidth = 128;
    private const int MinCell = 14;
    private const int MaxCell = 40;

    private Grid[] _modules = System.Array.Empty<Grid>();
    private string[] _moduleNames = System.Array.Empty<string>();
    private int _moduleIndex;
    private Vector2 _scroll;

    private MapBrush _brush = MapBrush.None;
    private int _cellPixels = 20;
    private bool _showProblems = true;
    private bool _showInert;
    private int _strokeGroup;
    private Vector2Int _hover = new(-1, -1);
    private string _targetSeen = string.Empty;

    [MenuItem("Tools/Map/Map Maker")]
    private static void Open()
    {
        GetWindow<MapMakerWindow>("Map Maker").minSize = new Vector2(420, 320);
    }

    private void OnEnable()
    {
        wantsMouseMove = true; // 아래 상태줄이 마우스를 따라가려면 이동 이벤트가 필요하다

        // Ctrl+Z는 데이터를 되돌리지만 이 창은 그 사실을 모른다 —
        // 다시 그리라고 알려주지 않으면 값은 돌아갔는데 화면은 그대로여서 "안 되돌아간다"로 보인다.
        Undo.undoRedoPerformed += Repaint;
        Refresh();
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= Repaint;
    }

    // 편집 대상이 바뀌면(씬 전환·프리팹 열기) 모듈 목록을 다시 잡는다.
    private void OnFocus()
    {
        Refresh();
    }

    private void Refresh()
    {
        List<Grid> found = ModuleScan.FindModules();
        _modules = found.ToArray();
        _moduleNames = new string[_modules.Length];

        for (int i = 0; i < _modules.Length; i++)
        {
            _moduleNames[i] = _modules[i].transform.root.name;
        }

        if (_moduleIndex >= _modules.Length)
        {
            _moduleIndex = 0;
        }
    }

    private void OnGUI()
    {
        // 편집 대상이 바뀌면(프리팹 열기·씬 전환) 창을 누르지 않아도 목록을 다시 잡는다.
        // 상단 표시는 매번 새로 읽는데 모듈 목록은 안 읽으면, 프리팹을 가리키면서 씬 모듈을 그리게 된다 —
        // 프리팹인 줄 알고 씬 인스턴스를 칠하는 바로 그 사고다.
        string target = TargetLabel();
        if (target != _targetSeen)
        {
            _targetSeen = target;
            Refresh();
        }

        DrawToolbar(target);

        if (_modules.Length == 0)
        {
            EditorGUILayout.HelpBox(
                "편집 대상에 Grid(모듈)가 없습니다. 씬을 열거나 모듈 프리팹을 연 뒤 새로고침을 누르세요.",
                MessageType.Info);
            return;
        }

        Grid module = _modules[_moduleIndex];
        if (module == null)
        {
            Refresh();
            return;
        }

        List<Tile> tiles = ModuleScan.CollectTiles(module);
        Dictionary<Vector2Int, Tile> cells = ModuleScan.MapCells(tiles, out _, out _);

        if (cells.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "이 모듈에 Tile 컴포넌트를 가진 타일이 없습니다. 큐브에 Tile을 붙인 뒤 " +
                "Tools/Map/Bake Tile Positions (Active Scene)로 좌표를 새기세요.",
                MessageType.Warning);
            return;
        }

        var view = new TileGridView(cells, _brush);
        List<LaneData> lanes = LaneQuery.BuildLanes(cells);

        // 씬 오버라이드는 씬 모드에서만 뜻이 있다 — 프리팹 스테이지는 비교할 프리팹이 없다.
        HashSet<Vector2Int> overrides = null;
        if (!ModuleScan.IsPrefabStage())
        {
            overrides = TileOverride.Collect(cells);
        }

        // 좁아지면 판을 하나씩 격자 아래로 내린다 — 격자는 마지막까지 자르지 않는다.
        float gridWidth = view.PixelWidth(_cellPixels);
        bool roomForLeft = position.width >= LeftWidth + gridWidth + 28;
        bool roomForBoth = position.width >= LeftWidth + gridWidth + RightWidth + 28;

        using (new EditorGUILayout.HorizontalScope())
        {
            if (roomForLeft)
            {
                DrawTerrainPanel(cells);
            }

            DrawGridArea(view, cells, lanes, overrides);

            if (roomForBoth)
            {
                DrawPlacePanel(cells);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (!roomForLeft)
            {
                DrawTerrainPanel(cells);
            }

            if (!roomForBoth)
            {
                DrawPlacePanel(cells);
            }
        }

        List<string> problems = TileAuthorRule.FindProblems(cells, lanes, tiles.Count);

        int overrideCount = overrides?.Count ?? 0;
        DrawStatusBar(cells, view, lanes, problems.Count, overrideCount);
        DrawMarkerLegend(overrideCount);
        DrawBrushNote();

        if (_showProblems)
        {
            DrawProblems(problems);
        }
    }

    // ---- 위: 대상 ----

    private void DrawToolbar(string target)
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            // 붓질 한 번 = 되돌리기 한 단계. 키보드 초점이 어디 있든 눌리도록 버튼으로도 둔다.
            if (GUILayout.Button("되돌리기", EditorStyles.toolbarButton, GUILayout.Width(56)))
            {
                Undo.PerformUndo();
            }

            if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(56)))
            {
                Refresh();
            }

            if (_modules.Length > 0)
            {
                _moduleIndex = EditorGUILayout.Popup(
                    _moduleIndex, _moduleNames, EditorStyles.toolbarPopup, GUILayout.Width(140));
            }

            GUILayout.Label(target, EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();

            GUILayout.Label("칸 크기", EditorStyles.miniLabel);
            _cellPixels = (int)GUILayout.HorizontalSlider(_cellPixels, MinCell, MaxCell, GUILayout.Width(60));

            _showProblems = GUILayout.Toggle(
                _showProblems, "검사", EditorStyles.toolbarButton, GUILayout.Width(40));

            // 붓과 무관하게 무효 조합 칸(지상의 CanRanged 등)을 격자에 상시 표시한다.
            _showInert = GUILayout.Toggle(
                _showInert, "무효", EditorStyles.toolbarButton, GUILayout.Width(40));
        }
    }

    /// <summary>
    /// 지금 무엇을 고치고 있는지. 모듈 이름은 프리팹이든 씬 인스턴스든 똑같이 MapModule_A로 나오므로,
    /// 대상 종류를 같이 띄우지 않으면 프리팹인 줄 알고 씬 인스턴스를 칠하는 사고가 난다
    /// (그 경우 오버라이드로만 남아 같은 프리팹을 쓰는 다른 씬에는 영영 반영되지 않는다).
    /// </summary>
    private static string TargetLabel()
    {
        if (ModuleScan.IsPrefabStage())
        {
            return $"◆ 프리팹 «{ModuleScan.TargetName()}» — 원본에 기록";
        }

        return $"○ 씬 «{ModuleScan.TargetName()}» — 오버라이드로 남음";
    }

    // ---- 왼쪽: 지형(큰 분류) ----

    private void DrawTerrainPanel(Dictionary<Vector2Int, Tile> cells)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(LeftWidth)))
        {
            GUILayout.Label("지형", EditorStyles.miniBoldLabel);
            TerrainRow(cells, MapBrush.Ground, "지상", TerrainType.Ground);
            TerrainRow(cells, MapBrush.High, "고지", TerrainType.High);
            TerrainRow(cells, MapBrush.Special, "외곽", TerrainType.Special);
            TerrainRow(cells, MapBrush.Core, "본진", TerrainType.Core);
            TerrainRow(cells, MapBrush.Empty, "빈(벽)", TerrainType.Empty);

            GUILayout.Space(6);
            GUILayout.Label("표식", EditorStyles.miniBoldLabel);
            BrushRow(MapBrush.Spawn, "스폰", MapMakerPalette.Spawn, TileTally.CountSpawn(cells));

            GUILayout.Space(6);
            BrushRow(MapBrush.None, "읽기만", MapMakerPalette.Panel, cells.Count);

            GUILayout.FlexibleSpace();
            GUILayout.Label("Alt+클릭 = 끄기", EditorStyles.miniLabel);
        }
    }

    private void TerrainRow(Dictionary<Vector2Int, Tile> cells, MapBrush brush, string label, TerrainType terrain)
    {
        BrushRow(brush, label, MapMakerPalette.Terrain(terrain), TileTally.CountTerrain(cells, terrain));
    }

    // ---- 오른쪽: 배치 허용(작은 분류)과 집계 ----

    private void DrawPlacePanel(Dictionary<Vector2Int, Tile> cells)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(RightWidth)))
        {
            GUILayout.Label("배치", EditorStyles.miniBoldLabel);
            BrushRow(MapBrush.Melee, "근접", MapMakerPalette.Mark, TileTally.CountFlag(cells, MapBrush.Melee));
            BrushRow(MapBrush.Ranged, "원거리", MapMakerPalette.Mark, TileTally.CountFlag(cells, MapBrush.Ranged));
            BrushRow(MapBrush.Build, "생산", MapMakerPalette.Mark, TileTally.CountFlag(cells, MapBrush.Build));

            GUILayout.Space(6);
            GUILayout.Label("집계", EditorStyles.miniBoldLabel);
            FlagBar(cells, MapBrush.Melee, "근접");
            FlagBar(cells, MapBrush.Ranged, "원거리");
            FlagBar(cells, MapBrush.Build, "생산");

            GUILayout.Space(6);
            GUILayout.Label("■ 켜짐   □ 켜졌지만\n     지금은 효과 없음", EditorStyles.miniLabel);
        }
    }

    /// <summary>
    /// 이 허용이 실제로 효력을 갖는 칸 수 대비 얼마나 찍혔는지.
    /// 분모는 지형이 정한다 — 근접·생산은 지상 칸이, 원거리는 고지 칸이 모집단이다.
    /// </summary>
    private void FlagBar(Dictionary<Vector2Int, Tile> cells, MapBrush brush, string label)
    {
        int inert = TileTally.CountInert(cells, brush);
        int working = TileTally.CountFlag(cells, brush) - inert;
        int field = TileTally.CountField(cells, brush);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(label, EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{working} / {field}", EditorStyles.miniLabel);
        }

        Color bar = MapMakerPalette.Dim(MapMakerPalette.Mark);
        if (_brush == brush)
        {
            bar = MapMakerPalette.Mark;
        }

        Rect track = GUILayoutUtility.GetRect(10f, 3f, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(track, MapMakerPalette.Panel);
        float ratio = Mathf.Clamp01(working / Mathf.Max(1f, field));
        EditorGUI.DrawRect(new Rect(track.x, track.y, track.width * ratio, track.height), bar);

        if (inert > 0)
        {
            GUILayout.Label($"↑ 효과 없는 {inert}칸", EditorStyles.miniLabel);
        }
    }

    // 색 조각 + 토글 + 개수 한 줄. GUI.backgroundColor로 버튼을 물들이지 않는다 —
    // 어두운 스킨의 버튼 텍스처에 곱해져 색끼리 다 비슷하게 탁해진다.
    private void BrushRow(MapBrush brush, string label, Color swatch, int count)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            Rect chip = GUILayoutUtility.GetRect(11f, 15f, GUILayout.Width(11));
            EditorGUI.DrawRect(new Rect(chip.x, chip.y + 2f, 11f, 11f), swatch);

            bool active = _brush == brush;
            bool pressed = GUILayout.Toggle(active, label, EditorStyles.miniButton, GUILayout.MinWidth(36));
            if (pressed)
            {
                _brush = brush;
            }

            GUILayout.Label(count.ToString(), EditorStyles.miniLabel, GUILayout.Width(24));
        }
    }

    // ---- 가운데: 격자 ----

    private void DrawGridArea(TileGridView view, Dictionary<Vector2Int, Tile> cells,
        IReadOnlyList<LaneData> lanes,
        HashSet<Vector2Int> overrides)
    {
        using (new EditorGUILayout.VerticalScope())
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            Rect area = GUILayoutUtility.GetRect(view.PixelWidth(_cellPixels), view.PixelHeight(_cellPixels));
            HandleHover(area, view);
            HandleStroke(area, view, cells);
            view.Draw(area, _cellPixels, lanes, _hover, overrides, _showInert);

            EditorGUILayout.EndScrollView();
        }
    }

    // ---- 아래: 지금 가리키는 칸 ----

    private void DrawStatusBar(
        Dictionary<Vector2Int, Tile> cells,
        TileGridView view,
        IReadOnlyList<LaneData> lanes,
        int problemCount, int overrideCount)
    {
        string reading = "칸 위에 마우스를 올리면 그 칸의 상태가 여기 나옵니다";
        bool hovering = cells.TryGetValue(_hover, out Tile tile);
        if (hovering)
        {
            reading = TileCellReadout.Describe(tile);
        }

        string pathText = LaneText(lanes);

        // 오버라이드는 씬 모드에서만 센다 — 있으면 프리팹과 다른 칸이 몇인지 늘 띄운다(6-3식 사고 조기 발견).
        string overrideText = string.Empty;
        if (overrideCount > 0)
        {
            overrideText = $" · 오버라이드 {overrideCount}칸";
        }

        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            GUILayout.Label(reading, EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label(
                $"{view.Cols}×{view.Rows} · 칸 {cells.Count} · 경로 {pathText} · 문제 {problemCount}{overrideText}",
                EditorStyles.miniLabel);
        }
    }

    private static string LaneText(IReadOnlyList<LaneData> lanes)
    {
        int valid = 0;
        int tiles = 0;

        for (int i = 0; i < lanes.Count; i++)
        {
            if (!lanes[i].IsValid)
            {
                continue;
            }

            valid++;
            tiles += lanes[i].Tiles.Count;
        }

        if (lanes.Count == 0)
        {
            return "없음";
        }

        return $"{valid}/{lanes.Count}개 · 총 {tiles}칸";
    }

    // 격자에 켜져 있는 상시 표식의 뜻을 한 줄로 알린다. 표식이 없으면 자리를 차지하지 않는다.
    private void DrawMarkerLegend(int overrideCount)
    {
        bool showOverride = overrideCount > 0;
        if (!showOverride && !_showInert)
        {
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (showOverride)
            {
                LegendChip(MapMakerPalette.Override, "프리팹과 다름");
            }

            if (_showInert)
            {
                LegendChip(MapMakerPalette.Inert, "무효 조합");
            }

            GUILayout.FlexibleSpace();
        }
    }

    private void LegendChip(Color color, string label)
    {
        Rect chip = GUILayoutUtility.GetRect(11f, 13f, GUILayout.Width(11));
        EditorGUI.DrawRect(new Rect(chip.x, chip.y + 2f, 9f, 9f), color);
        GUILayout.Label(label, EditorStyles.miniLabel);
    }

    // 겉보기와 뜻이 다른 붓 둘. 고른 동안만 띄운다.
    private void DrawBrushNote()
    {
        if (_brush == MapBrush.Empty)
        {
            EditorGUILayout.HelpBox(
                "Empty는 '빈 칸'이 아니라 벽입니다. 통행은 Ground와 Core만 가능하므로 High와 똑같이 길을 막습니다.",
                MessageType.Info);
        }

        if (_brush == MapBrush.Core)
        {
            EditorGUILayout.HelpBox(
                "Core는 색이 아니라 경로의 도착점입니다. 여럿 찍으면 가장 가까운 곳이 목표가 되고, 배치는 전부 막힙니다.",
                MessageType.Info);
        }
    }

    // ---- 입력 ----

    private void HandleHover(Rect area, TileGridView view)
    {
        Event input = Event.current;

        // 창을 벗어나면 가리키던 칸을 놓는다 — 안 놓으면 상태줄이 지난 칸을 계속 말한다.
        if (input.type == EventType.MouseLeaveWindow)
        {
            _hover = new Vector2Int(-1, -1);
            Repaint();
            return;
        }

        if (input.type != EventType.MouseMove)
        {
            return;
        }

        Vector2Int coord = view.CoordAt(input.mousePosition, area, _cellPixels);
        if (coord == _hover)
        {
            return;
        }

        _hover = coord;
        Repaint();
    }

    // 누른 순간 되돌리기 그룹을 열고, 끄는 동안 지나간 칸을 찍고, 뗄 때 한 단계로 접는다.
    private void HandleStroke(Rect area, TileGridView view, Dictionary<Vector2Int, Tile> cells)
    {
        if (_brush == MapBrush.None)
        {
            return; // 읽기 전용 모드 — 입력을 아예 받지 않는다
        }

        Event input = Event.current;
        if (input.button != 0)
        {
            return;
        }

        if (input.type == EventType.MouseDown)
        {
            _strokeGroup = TileStamp.BeginStroke();
            StampAt(input.mousePosition, area, view, cells, input.alt);
            input.Use();
            return;
        }

        if (input.type == EventType.MouseDrag)
        {
            StampAt(input.mousePosition, area, view, cells, input.alt);
            input.Use();
            return;
        }

        if (input.type == EventType.MouseUp)
        {
            TileStamp.EndStroke(_strokeGroup);
            input.Use();
        }
    }

    private void StampAt(Vector2 mouse, Rect area, TileGridView view, Dictionary<Vector2Int, Tile> cells,
        bool turnOff)
    {
        Vector2Int coord = view.CoordAt(mouse, area, _cellPixels);
        bool exists = cells.TryGetValue(coord, out Tile tile);
        if (!exists)
        {
            return; // 격자 밖이거나 타일이 없는 칸 — 1단계에서는 칸을 만들지 않는다
        }

        TileStamp.Stamp(tile, _brush, !turnOff);
        _hover = coord;
        Repaint(); // 경로가 바로 다시 계산돼 보이도록
    }

    // ---- 검사 ----

    private void DrawProblems(List<string> problems)
    {
        if (problems.Count == 0)
        {
            return; // 문제가 없으면 자리를 차지하지 않는다 — 격자에 한 줄이라도 더 준다
        }

        foreach (string problem in problems)
        {
            EditorGUILayout.HelpBox(problem, MessageType.Warning);
        }
    }
}
