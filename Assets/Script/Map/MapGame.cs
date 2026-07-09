using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MapGame : MonoBehaviour
{
    [System.Serializable]
    public class Placeable
    {
        public string label = "유닛";
        public GameObject prefab;
        public PlaceKind kind = PlaceKind.Unit;
    }

    private enum PlaceMode { Off, Place, Remove }

    [Header("References")]
    public MapBoard board;
    public IsoCamera isoCamera;

    [Header("Enemy")]
    public GameObject enemyPrefab;      // 비우면 캡슐 더미
    public float enemySpeed = 2f;
    public float enemyYOffset = 0.5f;
    public int waveCount = 5;
    public float waveInterval = 0.5f;

     
    public bool attachEnemyHitDummy = true;

    public float enemyDummyHp = 20f;
    public int enemyDummyDefense = 0;
 
    public int enemyDummyLayer = -1;

    [Header("사거리 뷰 (표시 전용)")]
    [Tooltip("배치 프리뷰/선택 시 보여줄 사거리(타일 칸 수). 실제 전투 사거리는 Hero의 트리거 콜라이더가 결정 — 이 값은 배치 계획용 시각화일 뿐이다.")]
    public int viewRange = 2;

    [Header("전투 테스트 (Hero 스탠드인)")]
    [Tooltip("켜면 배치되는 근접 유닛에 DummyMeleeAttacker를 붙여 Hero 방식(트리거+태그)으로 실제 데미지를 준다. Hero 완성되면 끈다.")]
    public bool attachMeleeAttacker = true;
    [Tooltip("공격자 사거리(월드 단위 = 트리거 반경).")]
    public float attackerRange = 2f;
    public int attackerPower = 5;
    public float attackerInterval = 0.5f;

    [Header("Placement")]
    [Tooltip("배치할 프리팹 목록. 비워두면 유닛/건물 더미 2종이 기본 제공된다.")]
    public List<Placeable> palette = new();
    public float placeYOffset = 0f;
    public bool showPath = true;

    /// <summary>배치 직후 호출(종류, 생성된 오브젝트, 타일). 팀원 로직 연결용 훅.</summary>
    public event System.Action<PlaceKind, GameObject, MapBoard.Cell> Placed;

    private Camera _cam;
    private PlaceMode _mode = PlaceMode.Off;
    private int _paletteIndex;
    private List<Placeable> _defaults;

    private readonly List<GameObject> _enemies = new();
    private readonly List<GameObject> _placed = new();

    private List<MapBoard.Cell> _pathCells;
    private readonly List<Vector3> _worldPath = new();
    private readonly HashSet<Vector2Int> _pathSet = new();

    private readonly List<Vector2Int> _tinted = new(); // 이번 프레임 임시 하이라이트(사거리/중심) — 다음 프레임에 원복
    private GameObject _selected;                       // 클릭으로 선택된 배치물(사거리 상시 표시)
    private bool _pointerOverPanel;
    private string _status = "";
    private Rect _panelRect = new(10, 10, 260, 560);

    private void Awake()
    {
        if (board == null) board = FindFirstObjectByType<MapBoard>();
        if (isoCamera == null) isoCamera = FindFirstObjectByType<IsoCamera>();
        _cam = Camera.main;
    }

    private void Start()
    {
        if (board == null)
        {
            _status = "MapBoard 없음";
            Debug.LogError("[MapGame] 씬에 MapBoard가 없습니다. 빈 오브젝트에 MapBoard 컴포넌트를 추가하세요.", this);
            return;
        }
        Repath();
    }

    private void Update()
    {
        if (_cam == null) _cam = Camera.main;
        HandleHotkeys();
        UpdateHover();
        HandlePlacementClick();
    }

    // ---- 경로 ----

    public void Repath()
    {
        ClearPathHighlight();
        _worldPath.Clear();
        _pathSet.Clear();

        _pathCells = null;
        if (board == null || !board.HasEndpoints)
        {
            _status = "스폰/본진을 찾지 못함";
            return;
        }

        _pathCells = board.FindEnemyPath();
        if (_pathCells == null)
        {
            _status = "경로 없음 (콘솔의 [MapBoard] 로그 확인)";
            return;
        }

        foreach (MapBoard.Cell cell in _pathCells)
        {
            _pathSet.Add(cell.coord);
            _worldPath.Add(cell.WorldTop);
        }

        _status = $"경로 {_pathCells.Count}칸";
        if (showPath) ApplyPathHighlight();
    }

    private void ApplyPathHighlight()
    {
        if (_pathCells == null) return;
        foreach (MapBoard.Cell cell in _pathCells)
            board.Highlight(cell.coord, board.pathColor);
    }

    private void ClearPathHighlight()
    {
        if (_pathCells == null || board == null) return;
        foreach (MapBoard.Cell cell in _pathCells)
            board.ClearHighlight(cell.coord);
    }

    // ---- 적 ----

    public void SpawnEnemy()
    {
        if (_worldPath.Count < 2)
        {
            _status = "경로가 없어 적을 낼 수 없음";
            return;
        }

        GameObject go = enemyPrefab != null
            ? Instantiate(enemyPrefab)
            : CreateDummy(PrimitiveType.Capsule, new Vector3(0.4f, 0.4f, 0.4f), new Color(0.88f, 0.33f, 0.24f), "Enemy");

        EnemyUnit unit = go.GetComponent<EnemyUnit>();
        if (unit == null) unit = go.AddComponent<EnemyUnit>();
        unit.SetPath(_worldPath, enemySpeed, enemyYOffset);

        if (attachEnemyHitDummy)
        {
            DummyDamageable dmg = go.GetComponent<DummyDamageable>();
            if (dmg == null) dmg = go.AddComponent<DummyDamageable>();
            dmg.maxHp = enemyDummyHp;
            dmg.defense = enemyDummyDefense;
            dmg.forceLayer = enemyDummyLayer;
            dmg.Initialize(); // 필드를 채운 뒤 즉시 반영(Start를 기다리지 않음)
        }

        _enemies.Add(go);
    }

    public void SpawnWave() => StartCoroutine(WaveRoutine());

    private IEnumerator WaveRoutine()
    {
        for (int i = 0; i < waveCount; i++)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(waveInterval);
        }
    }

    public void ClearEnemies()
    {
        foreach (GameObject e in _enemies) if (e != null) Destroy(e);
        _enemies.Clear();
    }

    // ---- 배치 ----

    /// <summary>등록된 팔레트(비어 있으면 더미 2종).</summary>
    private List<Placeable> Palette()
    {
        if (palette != null && palette.Count > 0) return palette;
        _defaults ??= new List<Placeable>
        {
            new() { label = "근접유닛(더미)", prefab = null, kind = PlaceKind.Unit },
            new() { label = "원거리(더미)", prefab = null, kind = PlaceKind.Ranged },
            new() { label = "건물(더미)", prefab = null, kind = PlaceKind.Building }
        };
        return _defaults;
    }

    private PlaceKind CurrentKind()
    {
        List<Placeable> pal = Palette();
        return (_paletteIndex >= 0 && _paletteIndex < pal.Count) ? pal[_paletteIndex].kind : PlaceKind.Unit;
    }

    private void UpdateHover()
    {
        // 지난 프레임에 칠한 임시 하이라이트(사거리/중심)를 모두 원복하고 이번 프레임 것을 다시 그린다.
        ClearTinted();

        // 1) 클릭으로 선택된 배치물이 있으면 그 사거리를 상시 표시(파괴됐으면 Unity 가짜 null로 자동 해제).
        if (_selected != null)
            ShowRange(board.WorldToCell(_selected.transform.position), viewRange, board.selectColor, skipCenter: false);

        // 2) 배치 모드에서 유닛을 갖다 대면 배치될 칸 주변 사거리를 미리 표시.
        if (_mode != PlaceMode.Place || _pointerOverPanel) return;
        MapBoard.Cell cell = RaycastCell();
        if (cell == null) return;

        PlaceKind kind = CurrentKind();
        if (kind == PlaceKind.Unit || kind == PlaceKind.Ranged)
            ShowRange(cell.coord, viewRange, board.rangeColor, skipCenter: true);

        // 중심 칸(배치 가능=초록 / 불가=빨강)을 사거리 위에 덮어 가장 잘 보이게.
        bool ok = board.CanPlace(cell.coord, kind, out _);
        board.Highlight(cell.coord, ok ? board.okColor : board.denyColor);
        _tinted.Add(cell.coord);
    }

    /// <summary>center 기준 range칸 타일을 color로 칠하고 이번 프레임 원복 목록에 등록한다.</summary>
    private void ShowRange(Vector2Int center, int range, Color color, bool skipCenter)
    {
        foreach (MapBoard.Cell c in board.TilesInRange(center, range, false))
        {
            if (skipCenter && c.coord == center) continue;
            board.Highlight(c.coord, color);
            _tinted.Add(c.coord);
        }
    }

    private void ClearTinted()
    {
        for (int i = 0; i < _tinted.Count; i++) RestoreCellColor(_tinted[i]);
        _tinted.Clear();
    }

    /// <summary>배치 모드가 아닐 때(Off) 배치물을 클릭하면 선택해 사거리(표시용)를 상시 표시한다.</summary>
    private void SelectAt(MapBoard.Cell cell)
    {
        _selected = cell.occupant;
        _status = _selected != null ? $"{cell.coord} 선택 — 표시 사거리 {viewRange}칸" : "선택 해제";
    }

    private void HandlePlacementClick()
    {
        if (_pointerOverPanel) return;
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;

        MapBoard.Cell cell = RaycastCell();
        if (cell == null) return;

        switch (_mode)
        {
            case PlaceMode.Remove: RemoveAt(cell); break;
            case PlaceMode.Place: PlaceAt(cell); break;
            default: SelectAt(cell); break; // Off: 유닛 클릭 → 사거리 표시
        }
    }

    private void PlaceAt(MapBoard.Cell cell)
    {
        List<Placeable> pal = Palette();
        if (_paletteIndex < 0 || _paletteIndex >= pal.Count) return;
        Placeable entry = pal[_paletteIndex];

        if (!board.CanPlace(cell.coord, entry.kind, out string reason))
        {
            _status = $"{cell.coord} {reason}";
            return;
        }

        GameObject go = entry.prefab != null ? Instantiate(entry.prefab) : DummyFor(entry.kind);

        if (board.TryPlace(cell.coord, go, entry.kind, placeYOffset, out reason))
        {
            _placed.Add(go);
            AttachMeleeAttacker(go, entry.kind);
            _status = $"{cell.coord}에 {entry.label} 배치";
            Placed?.Invoke(entry.kind, go, cell);
        }
        else
        {
            if (go != null) Destroy(go);
            _status = $"{cell.coord} {reason}";
        }
    }

    /// <summary>[테스트] 근접 유닛에 Hero 스탠드인 공격자를 붙인다(트리거+태그로 실제 데미지). 건물/원거리 제외.</summary>
    private void AttachMeleeAttacker(GameObject go, PlaceKind kind)
    {
        if (!attachMeleeAttacker || go == null || kind != PlaceKind.Unit) return;

        DummyMeleeAttacker atk = go.GetComponent<DummyMeleeAttacker>();
        if (atk == null) atk = go.AddComponent<DummyMeleeAttacker>(); // RequireComponent가 트리거 SphereCollider 자동 추가
        atk.range = attackerRange;
        atk.power = attackerPower;
        atk.attackInterval = attackerInterval;
        atk.Apply(); // 필드 채운 뒤 트리거 반경 반영
    }

    private void RemoveAt(MapBoard.Cell cell)
    {
        GameObject go = board.Vacate(cell.coord);
        if (go == null) return;
        _placed.Remove(go);
        Destroy(go);
        _status = $"{cell.coord} 제거";
    }

    public void ClearPlaced()
    {
        foreach (MapBoard.Cell cell in board.Cells.Values)
        {
            if (cell.occupant == null) continue;
            GameObject go = board.Vacate(cell.coord);
            if (go != null) Destroy(go);
        }
        _placed.Clear();
    }

    private GameObject DummyFor(PlaceKind kind) => kind switch
    {
        PlaceKind.Building => CreateDummy(PrimitiveType.Cube, new Vector3(0.7f, 0.5f, 0.7f), new Color(0.3f, 0.7f, 0.95f), "Building"),
        PlaceKind.Ranged => CreateDummy(PrimitiveType.Capsule, new Vector3(0.35f, 0.6f, 0.35f), new Color(0.85f, 0.4f, 0.95f), "Ranged"),
        _ => CreateDummy(PrimitiveType.Capsule, new Vector3(0.4f, 0.5f, 0.4f), new Color(0.95f, 0.9f, 0.35f), "Unit")
    };

    private MapBoard.Cell RaycastCell()
    {
        if (_cam == null || Mouse.current == null) return null;
        Ray ray = _cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        // 타일은 솔리드 콜라이더, 유닛 사거리는 트리거 → 트리거를 무시해야 사거리 구가
        // 타일 피킹을 가로채지 않는다(유닛이 타일 자식이라 CellFromCollider가 그 타일로 잘못 잡히던 버그).
        RaycastHit[] hits = Physics.RaycastAll(ray, 2000f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        MapBoard.Cell best = null;
        float bestDist = float.MaxValue;
        foreach (RaycastHit hit in hits)
        {
            MapBoard.Cell cell = board.CellFromCollider(hit.collider);
            if (cell != null && hit.distance < bestDist) { best = cell; bestDist = hit.distance; }
        }
        return best;
    }

    private void RestoreCellColor(Vector2Int coord)
    {
        if (showPath && _pathSet.Contains(coord)) board.Highlight(coord, board.pathColor);
        else board.ClearHighlight(coord);
    }

    private GameObject CreateDummy(PrimitiveType type, Vector3 scale, Color color, string name)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.localScale = scale;
        var col = go.GetComponent<Collider>();
        if (col != null) col.enabled = false; // 타일 레이캐스트를 방해하지 않도록
        var rend = go.GetComponent<Renderer>();
        if (rend != null) rend.material.color = color;
        return go;
    }

    private void Rescan()
    {
        ClearEnemies();
        ClearPlaced();
        board.Build();
        if (isoCamera != null) isoCamera.Frame();
        Repath();
    }

    // ---- 입력(단축키) ----

    private void HandleHotkeys()
    {
        Keyboard k = Keyboard.current;
        if (k == null) return;

        if (k.spaceKey.wasPressedThisFrame) SpawnEnemy();
        if (k.cKey.wasPressedThisFrame) ClearEnemies();
        if (k.pKey.wasPressedThisFrame) TogglePath();
        if (k.xKey.wasPressedThisFrame) _mode = PlaceMode.Remove;
        if (k.digit0Key.wasPressedThisFrame) SetModeOff();

        // 숫자 1~9 = 팔레트 항목 선택 + 배치 모드.
        Key[] digits =
        {
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
            Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
        };
        List<Placeable> pal = Palette();
        for (int i = 0; i < digits.Length && i < pal.Count; i++)
            if (k[digits[i]].wasPressedThisFrame) { _paletteIndex = i; _mode = PlaceMode.Place; }
    }

    private void TogglePath()
    {
        showPath = !showPath;
        if (showPath) ApplyPathHighlight(); else ClearPathHighlight();
    }

    private void SetModeOff()
    {
        _mode = PlaceMode.Off;
        _selected = null; // 배치 끄면 선택도 해제(다음 프레임 ClearTinted가 원복)
    }

    // ---- GUI ----

    private void OnGUI()
    {
        if (Event.current.type == EventType.Repaint)
        {
            Vector2 m = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            _pointerOverPanel = _panelRect.Contains(new Vector2(m.x, Screen.height - m.y));
        }

        GUILayout.BeginArea(_panelRect, GUI.skin.box);
        GUILayout.Label("<b>맵 테스트</b>", RichLabel());
        GUILayout.Label(_status);

        GUILayout.Space(6);
        GUILayout.Label($"경로 표시: {(showPath ? "ON" : "OFF")}");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("경로 재계산")) Repath();
        if (GUILayout.Button(showPath ? "경로 끄기" : "경로 켜기")) TogglePath();
        GUILayout.EndHorizontal();

        GUILayout.Space(6);
        GUILayout.Label($"적 (속도 {enemySpeed:0.0})");
        enemySpeed = GUILayout.HorizontalSlider(enemySpeed, 0.5f, 6f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("적 1마리 (Space)")) SpawnEnemy();
        if (GUILayout.Button($"웨이브 {waveCount}")) SpawnWave();
        GUILayout.EndHorizontal();
        if (GUILayout.Button("적 전멸 (C)")) ClearEnemies();

        GUILayout.Space(6);
        GUILayout.Label($"배치 모드: {_mode}");
        List<Placeable> pal = Palette();
        for (int i = 0; i < pal.Count; i++)
        {
            bool sel = _mode == PlaceMode.Place && _paletteIndex == i;
            if (GUILayout.Button($"{(sel ? "▶ " : "")}{pal[i].label} ({i + 1})"))
            {
                _paletteIndex = i;
                _mode = PlaceMode.Place;
            }
        }
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("제거 (X)")) _mode = PlaceMode.Remove;
        if (GUILayout.Button("끄기 (0)")) SetModeOff();
        GUILayout.EndHorizontal();
        if (GUILayout.Button("배치 전부 제거")) ClearPlaced();

        GUILayout.Space(4);
        GUILayout.Label($"<color=#4d99ff>■</color> 갖다댈 때 = 파란 사거리 프리뷰\n" +
                        $"<color=#9e7af9>■</color> 끄기(0) 후 유닛 클릭 = 사거리 표시", RichLabel());

        GUILayout.Space(6);
        if (GUILayout.Button("보드 다시 스캔")) Rescan();

        GUILayout.EndArea();
    }

    private static GUIStyle RichLabel() => new(GUI.skin.label) { richText = true };
}
