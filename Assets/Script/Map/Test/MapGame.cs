using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class MapGame : MonoBehaviour
{
    [System.Serializable]
    public class Placeable
    {
        public string label = "유닛";
        public GameObject prefab;
        public OccupantKind kind = OccupantKind.MeleeHero;
    }

    private enum PlaceMode { Off, Place, Remove }

    [Header("References")]
    public MapBoard board;
    public IsoCamera isoCamera;
    [Tooltip("타일 색칠(검증용 시각화). 비우면 색 피드백만 꺼지고 게임 로직은 정상. 나중에 통째 제거 대상.")]
    public TilePainter painter;

    [Header("Enemy")]
    public GameObject enemyPrefab;      // 비우면 캡슐 더미
    public float enemySpeed = 2f;
    public float enemyYOffset = 0.5f;
    public int waveCount = 5;
    public float waveInterval = 0.5f;

    public bool attachEnemyHitDummy = true;

    public float enemyDummyHp = 20f;
    public int enemyDummyDefense = 0;

    [Header("사거리 폴백 (스탯 컴포넌트 없는 더미용)")]
    [Tooltip("사거리의 단일 출처는 유닛 프리팹의 IUnitStats.AttackRange. 이 값은 IUnitStats가 없는 더미(원거리 등)의 폴백 표시/커버리지 사거리(타일 칸).")]
    public int viewRange = 2;

    [Header("전투 테스트 (Hero 스탠드인)")]
    [Tooltip("켜면 배치되는 근접 유닛에 타일 기반 DummyMeleeAttacker를 붙여 실제 데미지를 준다. Hero 완성되면 끈다.")]
    public bool attachMeleeAttacker = true;
    [Tooltip("근접 더미(IUnitStats 없는 캡슐)의 폴백 사거리(타일 칸). 실제 유닛은 프리팹의 IUnitStats.AttackRange가 우선.")]
    public float attackerRange = 2f;
    public int attackerPower = 5;
    public float attackerInterval = 0.5f;
    [Tooltip("근접 유닛 1기가 같은 타일에서 동시에 저지할 수 있는 적 수.")]
    public int meleeBlockCapacity = 1;
    [Tooltip("공격 선 표시용 머티리얼(Game뷰). 배치되는 근접 공격자에 주입된다. 비우면 선 미표시.")]
    public Material attackLineMaterial;

    [Tooltip("켜면 배치되는 원거리 유닛에 타일 기반 DummyRangedAttacker를 붙여 실제 데미지를 준다. Hero 완성되면 끈다.")]
    public bool attachRangedAttacker = true;
    public int rangedPower = 4;
    public float rangedInterval = 0.7f;
    [Tooltip("원거리 공격 선 머티리얼(Game뷰). 비우면 attackLineMaterial을 재사용.")]
    public Material rangedLineMaterial;

    [Header("Placement")]
    [Tooltip("배치할 프리팹 목록. 비워두면 유닛/건물 더미 3종이 기본 제공된다.")]
    public List<Placeable> palette = new();
    public float placeYOffset = 0f;
    public bool showPath = true;

    [Header("타일 상태 피드백 (검증용, Game뷰)")]
    [Tooltip("적이 올라온 타일 색칠.")]
    public bool showEnemyTiles = true;
    [Tooltip("아군+적이 겹친(저지 중) 타일 색칠.")]
    public bool showBlocking = true;
    [Tooltip("지상유닛 배치 + 적 저지중 + 원거리 사거리 포함 = 세 조건 동시 성립 타일을 콘솔에 로그(검증용).")]
    public bool logCombatTiles = true;

    /// <summary>배치 직후 호출(종류, 생성된 오브젝트, 타일). 팀원 로직 연결용 훅.</summary>
    public event System.Action<OccupantKind, GameObject, Tile> Placed;

    private Camera _cam;
    private PlaceMode _mode = PlaceMode.Off;
    private int _paletteIndex;
    private List<Placeable> _defaults;

    private readonly List<GameObject> _enemies = new();
    private readonly List<GameObject> _placed = new();
    private readonly HashSet<GameObject> _ranged = new();      // 배치된 원거리 유닛(커버리지 구분용, 검증)
    private readonly HashSet<Vector2Int> _combatLogged = new(); // 이미 로그한 교전 타일(엣지 감지, 중복 로그 방지)

    private List<Tile> _pathCells;
    private readonly List<Vector3> _worldPath = new();
    private readonly HashSet<Vector2Int> _pathSet = new();

    private readonly List<Vector2Int> _tinted = new(); // 이번 프레임 임시 하이라이트(사거리/중심) — 다음 프레임에 원복
    private Tile _selectedTile;                         // 클릭으로 선택된 타일(정보 패널용, 사거리와 무관)
    private bool _pointerOverPanel;
    private string _status = "";
    private const float PanelWidth = 230f;            // 두 패널 공통 폭
    private Rect _infoRect;                            // 왼쪽 위: 읽기 전용 정보(상태·선택 타일)
    private Rect _ctrlRect;                            // 오른쪽 끝: 조작 버튼(경로·적·배치)

    private void Awake()
    {
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
        board.EnemyEntered += OnEnemyTileChanged;
        board.EnemyExited += OnEnemyTileChanged;
        Repath();
    }

    private void OnDestroy()
    {
        if (board == null) return;
        board.EnemyEntered -= OnEnemyTileChanged;
        board.EnemyExited -= OnEnemyTileChanged;
    }

    private void Update()
    {
        if (_cam == null) _cam = Camera.main;
        HandleHotkeys();
        UpdateHover();
        HandlePlacementClick();
        CheckCombatTiles();
    }

    // 검증 로그: 지상유닛 배치 + 적 저지중 + 원거리 사거리 포함이 동시에 성립하는 타일을 콘솔에 알림.
    // 상태에 새로 진입할 때 1회만 로그(매 프레임 도배 방지). 원거리 데미지 로직 없이도 커버리지만으로 판정.
    private void CheckCombatTiles()
    {
        if (!logCombatTiles || board == null) return;

        foreach (Tile tile in board.Cells.Values)
        {
            bool hit = tile.State.Occupant == OccupantKind.MeleeHero // 지상유닛 배치
                    && tile.BlockedCount > 0                          // 적 저지중
                    && CoveredByRanged(tile);                         // 원거리 사거리 포함

            if (hit)
            {
                if (_combatLogged.Add(tile.Coord))
                    Debug.Log($"[교전] {tile.Coord} 지상유닛 저지 {tile.BlockedCount}마리 + 원거리 사거리 포함", this);
            }
            else
            {
                _combatLogged.Remove(tile.Coord); // 조건 해제 → 다음 진입 때 다시 로그
            }
        }
    }

    // 타일이 배치된 원거리 유닛의 커버리지에 드는가(커버리지 등록은 배치 시 이미 됨).
    private bool CoveredByRanged(Tile tile)
    {
        foreach (GameObject coverer in tile.Covers)
        {
            if (coverer != null && _ranged.Contains(coverer)) return true;
        }
        return false;
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

        _pathCells = board.GetPath();
        if (_pathCells == null)
        {
            _status = "경로 없음 (콘솔의 [MapBoard] 로그 확인)";
            return;
        }

        foreach (Tile tile in _pathCells)
        {
            _pathSet.Add(tile.Coord);
            _worldPath.Add(tile.WorldTop);
        }

        _status = $"경로 {_pathCells.Count}칸";
        if (showPath) ApplyPathHighlight();
    }

    private void ApplyPathHighlight()
    {
        if (_pathCells == null || painter == null) return;
        foreach (Tile tile in _pathCells)
            painter.SetColor(tile.Coord, painter.pathColor);
    }

    private void ClearPathHighlight()
    {
        if (_pathCells == null || painter == null) return;
        foreach (Tile tile in _pathCells)
            painter.ClearColor(tile.Coord);
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
        unit.board = board; // 격자 점유 보고 대상 연결
        unit.SetPath(_worldPath, enemySpeed, enemyYOffset, _pathCells); // 칸 판정은 경로 타일 인덱스 기준

        if (attachEnemyHitDummy)
        {
            DummyDamageable dmg = go.GetComponent<DummyDamageable>();
            if (dmg == null) dmg = go.AddComponent<DummyDamageable>();
            dmg.maxHp = enemyDummyHp;
            dmg.defense = enemyDummyDefense;
            dmg.Initialize(); // 필드를 채운 뒤 즉시 반영(Start를 기다리지 않음)
        }

        _enemies.Add(go);
    }

    public void SpawnWave() => WaveRoutine().Forget();

    // 오브젝트 파괴 시 자동 취소(코루틴이 destroy에서 멈추던 동작과 동일). 씬 전환·정리 중 스폰 방지.
    private async UniTaskVoid WaveRoutine()
    {
        CancellationToken token = this.GetCancellationTokenOnDestroy();
        for (int i = 0; i < waveCount; i++)
        {
            SpawnEnemy();
            await UniTask.Delay(TimeSpan.FromSeconds(waveInterval), cancellationToken: token);
        }
    }

    public void ClearEnemies()
    {
        foreach (GameObject e in _enemies) if (e != null) Destroy(e);
        _enemies.Clear();
    }

    // ---- 배치 ----

    /// <summary>등록된 팔레트(비어 있으면 더미 3종).</summary>
    private List<Placeable> Palette()
    {
        if (palette != null && palette.Count > 0) return palette;
        _defaults ??= new List<Placeable>
        {
            new() { label = "근접유닛(더미)", prefab = null, kind = OccupantKind.MeleeHero },
            new() { label = "원거리(더미)", prefab = null, kind = OccupantKind.RangedHero },
            new() { label = "건물(더미)", prefab = null, kind = OccupantKind.Building }
        };
        return _defaults;
    }

    private OccupantKind CurrentKind()
    {
        List<Placeable> pal = Palette();
        return (_paletteIndex >= 0 && _paletteIndex < pal.Count) ? pal[_paletteIndex].kind : OccupantKind.MeleeHero;
    }

    private void UpdateHover()
    {
        // 지난 프레임에 칠한 임시 하이라이트(사거리/중심)를 모두 원복하고 이번 프레임 것을 다시 그린다.
        if (painter == null) return; // 시각화 없음 → 색칠 전부 생략(게임 로직은 클릭 처리에서 계속)

        ClearTinted();
        PaintState(); // 적/저지 상태를 매 프레임 색으로 표시(검증용). 사거리는 호버 시에만.

        if (_pointerOverPanel) return;
        Tile tile = PickCellUnderPointer();
        if (tile == null) return;

        // 1) 배치 모드에서 유닛을 갖다 대면 배치될 칸 주변 사거리를 미리 표시.
        if (_mode == PlaceMode.Place)
        {
            OccupantKind kind = CurrentKind();
            if (kind == OccupantKind.MeleeHero || kind == OccupantKind.RangedHero)
                ShowRange(tile.Coord, PreviewRange(kind), painter.rangeColor, skipCenter: true);

            // 중심 칸(배치 가능=초록 / 불가=빨강)을 사거리 위에 덮어 가장 잘 보이게.
            bool ok = board.CanPlace(tile.Coord, kind, out _);
            painter.SetColor(tile.Coord, ok ? painter.okColor : painter.denyColor);
            _tinted.Add(tile.Coord);
            return;
        }

        // 2) 배치 모드가 아닐 때: 배치된 유닛에 마우스를 올린 동안에만 그 유닛 사거리 표시. 벗어나면 자동 원복.
        if (tile.OccupantObject != null)
        {
            int range = AttackRangeFor(tile.OccupantObject, tile.State.Occupant);
            if (range >= 0)
                ShowRange(tile.Coord, range, painter.rangeColor, skipCenter: true);
        }
    }

    /// <summary>center 기준 range칸 타일을 color로 칠하고 이번 프레임 원복 목록에 등록한다.</summary>
    private void ShowRange(Vector2Int center, int range, Color color, bool skipCenter)
    {
        foreach (Tile t in board.GetTiles(center, range, false))
        {
            if (skipCenter && t.Coord == center) continue;
            painter.SetColor(t.Coord, color);
            _tinted.Add(t.Coord);
        }
    }

    private void ClearTinted()
    {
        for (int i = 0; i < _tinted.Count; i++) RestoreCellColor(_tinted[i]);
        _tinted.Clear();
    }

    // 타일 상태를 매 프레임 색으로 표시(검증용). 우선순위: 겹침(저지) > 적. 사거리는 호버/선택 시에만.
    private void PaintState()
    {
        foreach (Tile tile in board.Cells.Values)
        {
            Color color;
            if (showBlocking && !tile.IsEmpty && tile.HasEnemy)
            {
                color = painter.blockColor; // 아군+적 = 오브젝트 2개 이상
            }
            else if (showEnemyTiles && tile.HasEnemy)
            {
                color = painter.enemyColor;
            }
            else
            {
                continue;
            }

            painter.SetColor(tile.Coord, color);
            _tinted.Add(tile.Coord);
        }
    }

    /// <summary>배치 모드가 아닐 때(Off) 타일을 클릭하면 정보 패널용으로 선택한다. 사거리는 호버로만 표시.</summary>
    private void SelectAt(Tile tile)
    {
        _selectedTile = tile;
        _status = tile.OccupantObject != null ? $"{tile.Coord} 선택 ({tile.OccupantObject.name})" : $"{tile.Coord} 타일 선택";
    }

    private void HandlePlacementClick()
    {
        if (_pointerOverPanel) return;
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;

        Tile tile = PickCellUnderPointer();
        if (tile == null) return;

        switch (_mode)
        {
            case PlaceMode.Remove: RemoveAt(tile); break;
            case PlaceMode.Place: PlaceAt(tile); break;
            default: SelectAt(tile); break; // Off: 타일 클릭 → 정보 패널 선택
        }
    }

    private void PlaceAt(Tile tile)
    {
        List<Placeable> pal = Palette();
        if (_paletteIndex < 0 || _paletteIndex >= pal.Count) return;
        Placeable entry = pal[_paletteIndex];

        if (!board.CanPlace(tile.Coord, entry.kind, out string reason))
        {
            _status = $"{tile.Coord} {reason}";
            return;
        }

        GameObject go = entry.prefab != null ? Instantiate(entry.prefab) : DummyFor(entry.kind);
        EnsureMeleeBlocker(go, entry.kind);

        if (board.TryPlace(tile.Coord, go, entry.kind, placeYOffset, out reason))
        {
            _placed.Add(go);
            if (entry.kind == OccupantKind.RangedHero) _ranged.Add(go); // 커버리지 원거리 구분(검증)
            AttachMeleeAttacker(go, entry.kind);
            AttachRangedAttacker(go, entry.kind);
            RegisterCover(go, tile, entry.kind);
            _selectedTile = tile; // 정보 패널만 갱신(사거리 상시표시 아님)
            _status = $"{tile.Coord}에 {entry.label} 배치";
            Placed?.Invoke(entry.kind, go, tile);
        }
        else
        {
            if (go != null) Destroy(go);
            _status = $"{tile.Coord} {reason}";
        }
    }

    private void EnsureMeleeBlocker(GameObject go, OccupantKind kind)
    {
        if (go == null || kind != OccupantKind.MeleeHero) return;
        if (go.GetComponent<TileBlocker>() != null) return;

        TileBlocker blocker = go.AddComponent<TileBlocker>();
        blocker.blockCapacity = Mathf.Max(0, meleeBlockCapacity);
    }

    /// <summary>[테스트] 근접 유닛에 타일 기반 Hero 스탠드인 공격자를 붙인다. 건물/원거리 제외.</summary>
    private void AttachMeleeAttacker(GameObject go, OccupantKind kind)
    {
        if (!attachMeleeAttacker || go == null || kind != OccupantKind.MeleeHero) return;

        DummyMeleeAttacker atk = go.GetComponent<DummyMeleeAttacker>();
        if (atk == null) atk = go.AddComponent<DummyMeleeAttacker>();
        atk.range = attackerRange;
        atk.power = attackerPower;
        atk.attackInterval = attackerInterval;
        atk.board = board; // 자동탐색 대신 주입
        atk.lineMaterial = attackLineMaterial; // 공격 선 머티리얼 주입
    }

    /// <summary>[테스트] 원거리 유닛에 타일 기반 Hero 스탠드인 공격자를 붙인다. 근접/건물 제외.</summary>
    private void AttachRangedAttacker(GameObject go, OccupantKind kind)
    {
        if (!attachRangedAttacker || go == null || kind != OccupantKind.RangedHero) return;

        DummyRangedAttacker atk = go.GetComponent<DummyRangedAttacker>();
        if (atk == null) atk = go.AddComponent<DummyRangedAttacker>();
        atk.range = AttackRangeFor(go, kind); // 사거리는 IUnitStats 우선, 없으면 viewRange 폴백
        atk.power = rangedPower;
        atk.attackInterval = rangedInterval;
        atk.board = board; // 자동탐색 대신 주입
        atk.lineMaterial = rangedLineMaterial != null ? rangedLineMaterial : attackLineMaterial;
    }

    private void RegisterCover(GameObject go, Tile tile, OccupantKind kind)
    {
        if (board == null || go == null || tile == null) return;

        int range = AttackRangeFor(go, kind);
        if (range < 0) return;

        board.SetCover(go, tile.Coord, range);
    }

    /// <summary>유닛의 공격 사거리(타일 칸). 유닛 프리팹의 IUnitStats가 단일 출처, 없으면 더미 폴백.</summary>
    private int AttackRangeFor(GameObject go, OccupantKind kind)
    {
        if (go != null && go.GetComponentInChildren<IUnitStats>() is IUnitStats stats)
            return Mathf.Max(0, stats.AttackRange);

        // 스탯 컴포넌트 없는 더미 폴백(임시 검증용).
        if (kind == OccupantKind.MeleeHero) return Mathf.Max(0, Mathf.RoundToInt(attackerRange));
        if (kind == OccupantKind.RangedHero) return Mathf.Max(0, viewRange);
        return -1;
    }

    /// <summary>배치 프리뷰용 사거리. 팔레트 프리팹의 IUnitStats를 읽어 표시=실제를 맞춘다. 없으면 더미 폴백.</summary>
    private int PreviewRange(OccupantKind kind)
    {
        List<Placeable> pal = Palette();
        GameObject prefab = (_paletteIndex >= 0 && _paletteIndex < pal.Count) ? pal[_paletteIndex].prefab : null;
        if (prefab != null && prefab.GetComponentInChildren<IUnitStats>() is IUnitStats stats)
            return Mathf.Max(0, stats.AttackRange);

        if (kind == OccupantKind.MeleeHero) return Mathf.Max(0, Mathf.RoundToInt(attackerRange));
        return Mathf.Max(0, viewRange);
    }

    private void RemoveAt(Tile tile)
    {
        GameObject go = board.Vacate(tile.Coord);
        if (go == null) return;
        _placed.Remove(go);
        _ranged.Remove(go);
        Destroy(go);
        if (_selectedTile == tile)
        {
            _selectedTile = null;
        }
        _status = $"{tile.Coord} 제거";
    }

    public void ClearPlaced()
    {
        foreach (Tile tile in board.Cells.Values)
        {
            if (tile.OccupantObject == null) continue;
            GameObject go = board.Vacate(tile.Coord);
            if (go != null) Destroy(go);
        }
        _placed.Clear();
        _ranged.Clear();
        _combatLogged.Clear();
    }

    private GameObject DummyFor(OccupantKind kind) => kind switch
    {
        OccupantKind.Building => CreateDummy(PrimitiveType.Cube, new Vector3(0.7f, 0.5f, 0.7f), new Color(0.3f, 0.7f, 0.95f), "Building"),
        OccupantKind.RangedHero => CreateDummy(PrimitiveType.Capsule, new Vector3(0.35f, 0.6f, 0.35f), new Color(0.85f, 0.4f, 0.95f), "Ranged"),
        _ => CreateDummy(PrimitiveType.Capsule, new Vector3(0.4f, 0.5f, 0.4f), new Color(0.95f, 0.9f, 0.35f), "Unit")
    };

    private Tile PickCellUnderPointer()
    {
        if (_cam == null || Mouse.current == null) return null;
        Ray ray = _cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        // 타일별 실제 윗면 높이로 정확 피킹(High 등 높이가 달라도 윗면 클릭이 맞는다).
        return board.CellFromRay(ray);
    }

    private void RestoreCellColor(Vector2Int coord)
    {
        if (showPath && _pathSet.Contains(coord)) painter.SetColor(coord, painter.pathColor);
        else painter.ClearColor(coord);
    }

    private GameObject CreateDummy(PrimitiveType type, Vector3 scale, Color color, string name)
    {
        GameObject go = new(name);
        go.name = name;
        go.transform.localScale = scale;

        MeshFilter filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = BuiltinMesh(type);

        MeshRenderer rend = go.AddComponent<MeshRenderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            Material material = new(shader) { color = color };
            rend.sharedMaterial = material;
        }

        return go;
    }

    private static Mesh BuiltinMesh(PrimitiveType type)
    {
        string meshName = type switch
        {
            PrimitiveType.Capsule => "Capsule.fbx",
            PrimitiveType.Cylinder => "Cylinder.fbx",
            PrimitiveType.Sphere => "Sphere.fbx",
            PrimitiveType.Plane => "Plane.fbx",
            PrimitiveType.Quad => "Quad.fbx",
            _ => "Cube.fbx"
        };

        return Resources.GetBuiltinResource<Mesh>(meshName);
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

        // 숫자 1부터 9 = 팔레트 항목 선택 + 배치 모드.
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
    }

    private void OnEnemyTileChanged(Tile tile)
    {
        if (tile == null || !tile.IsBlocking) return;

        _status = $"{tile.Coord} 저지 중 {tile.BlockedCount}/{tile.BlockCapacity} (타일 위 적 {tile.EnemyCount})";
        Debug.Log($"{tile.Coord} 저지 중: {tile.BlockedCount}/{tile.BlockCapacity}, 타일 위 적 {tile.EnemyCount}", this);
    }

    private string SelectedTileInfo()
    {
        if (_selectedTile == null) return "";

        string occupant = _selectedTile.State.Occupant == OccupantKind.None
            ? "없음"
            : _selectedTile.State.Occupant.ToString();

        return $"선택 타일 {_selectedTile.Coord}\n" +
               $"지형: {_selectedTile.Terrain}\n" +
               $"적 경로: {(_selectedTile.IsEnemyLane ? "포함" : "아님")}\n" +
               $"배치: {occupant}\n" +
               $"적: {_selectedTile.EnemyCount}\n" +
               $"아군 공격범위: {(_selectedTile.IsCovered ? $"덮임({_selectedTile.CoverCount})" : "없음")}\n" +
               $"저지: {_selectedTile.BlockedCount}/{_selectedTile.BlockCapacity}\n" +
               $"남은 저지: {_selectedTile.FreeBlock}";
    }

    // ---- GUI ----

    private void OnGUI()
    {
        // 화면 양쪽 가장자리에 도킹(가운데 맵을 가리지 않도록). 해상도 바뀌어도 따라감.
        _infoRect = new Rect(10, 10, PanelWidth, 300);
        _ctrlRect = new Rect(Screen.width - PanelWidth - 10, 10, PanelWidth, 560);

        if (Event.current.type == EventType.Repaint)
        {
            Vector2 m = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            Vector2 p = new(m.x, Screen.height - m.y);
            _pointerOverPanel = _infoRect.Contains(p) || _ctrlRect.Contains(p);
        }

        DrawInfoPanel();
        DrawControlPanel();
    }

    // 읽기 전용 정보 패널(상태·선택 타일). 조작은 DrawControlPanel.
    private void DrawInfoPanel()
    {
        GUILayout.BeginArea(_infoRect, GUI.skin.box);
        GUILayout.Label("<b>맵 정보</b>", RichLabel());
        GUILayout.Label(_status);
        string selectedInfo = SelectedTileInfo();
        if (!string.IsNullOrEmpty(selectedInfo))
        {
            GUILayout.Space(4);
            GUILayout.Label(selectedInfo);
        }
        GUILayout.EndArea();
    }

    // 조작 버튼 패널(경로·적·배치).
    private void DrawControlPanel()
    {
        GUILayout.BeginArea(_ctrlRect, GUI.skin.box);
        GUILayout.Label("<b>맵 테스트</b>", RichLabel());
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
        GUILayout.Label($"<color=#4d99ff>■</color> 배치 갖다댈 때 = 파란 사거리 프리뷰\n" +
                        $"<color=#4d99ff>■</color> 끄기(0) 후 유닛에 마우스 올리면 = 사거리 표시", RichLabel());

        GUILayout.Space(6);
        if (GUILayout.Button("보드 다시 스캔")) Rescan();

        GUILayout.EndArea();
    }

    private static GUIStyle RichLabel() => new(GUI.skin.label) { richText = true };
}
