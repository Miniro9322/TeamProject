using System.Collections.Generic;
using UnityEngine;


public class MapBoard : MonoBehaviour
{
    public class Cell
    {
        public Vector2Int coord;
        public TileKind kind;
        public Transform root;
        public Renderer[] renderers;
        public float topY;
        public GameObject occupant;

        public Vector3 WorldTop => new(root.position.x, topY, root.position.z);
        public bool IsEmpty => occupant == null;
        public bool Walkable => kind != TileKind.Border && kind != TileKind.High;
    }

    [Header("Scan")]
    [Tooltip("타일로 인식할 큐브 이름 접두사.")]
    public string cubePrefix = "Cube_";
    [Tooltip("고지대로 취급할 큐브 이름(접두사) 목록. 예: Cube_Rock. 이름으로도 고지 지정 가능(선택).")]
    public List<string> highCubePrefixes = new();
    [Tooltip("켜면 바닥보다 솟아 있는 큐브를 이름과 무관하게 고지(High)로 인식한다. 고지를 '큐브를 띄워' 만드는 방식에 맞춤.")]
    public bool detectHighByElevation = true;
    [Tooltip("바닥 높이보다  높으면 고지로 판정.")]
    [Range(0.1f, 1f)] public float highRaiseFactor = 0.4f;

    [Header("Highlight Colors")]
    public Color pathColor = new(0.22f, 0.85f, 0.54f);
    public Color okColor = new(0.21f, 0.77f, 0.41f);
    public Color denyColor = new(0.85f, 0.29f, 0.27f);
    [Tooltip("배치 프리뷰 시 유닛 사거리 타일 색.")]
    public Color rangeColor = new(0.30f, 0.60f, 1f);
    [Tooltip("배치된 유닛을 클릭했을 때 사거리 타일 색.")]
    public Color selectColor = new(0.62f, 0.48f, 0.98f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly Vector2Int[] Dirs = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };

    private readonly Dictionary<Vector2Int, Cell> _cells = new();
    private readonly Dictionary<Transform, Cell> _byRoot = new();
    private readonly List<Cell> _spawns = new();
    private readonly List<Cell> _cores = new();
    private MaterialPropertyBlock _mpb;

    private float _cellSize = 1f;
    private float _originX, _originZ; // (0,0)칸의 월드 x,z — 월드↔칸 변환 기준
    private Bounds _worldBounds;

    public IReadOnlyDictionary<Vector2Int, Cell> Cells => _cells;
    public int CellCount => _cells.Count;
    public IReadOnlyList<Cell> Spawns => _spawns;
    public IReadOnlyList<Cell> Cores => _cores;
    public bool HasEndpoints => _spawns.Count > 0 && _cores.Count > 0;
    public Bounds WorldBounds => _worldBounds;
    public float CellSize => _cellSize;

     
    public event System.Action<Cell> Occupied;
 
    public event System.Action<Cell> Vacated;
 
    public System.Func<Cell, PlaceKind, bool> placementRule;

    private void Awake() => Build();

    [ContextMenu("Build")]
    public void Build()
    {
        _cells.Clear();
        _byRoot.Clear();
        _spawns.Clear();
        _cores.Clear();
        _mpb ??= new MaterialPropertyBlock();

        List<Transform> cubes = FindCubeRoots();
        if (cubes.Count == 0)
        {
            Debug.LogWarning($"[MapBoard] '{cubePrefix}'로 시작하는 큐브를 찾지 못했습니다.", this);
            return;
        }

        // 1) 셀 크기 = 큐브 간 최근접 거리의 중앙값(격자 간격). 축 스냅에 쓴다.
        var positions = new List<Vector3>(cubes.Count);
        foreach (Transform t in cubes) positions.Add(t.position);
        _cellSize = EstimateCellSize(positions);

        float minX = float.MaxValue, minZ = float.MaxValue;
        foreach (Vector3 p in positions) { minX = Mathf.Min(minX, p.x); minZ = Mathf.Min(minZ, p.z); }
        _originX = minX;
        _originZ = minZ;

        // 2) 큐브마다 Cell 생성 + 좌표 스냅.
        bool hasBounds = false;
        for (int i = 0; i < cubes.Count; i++)
        {
            Transform root = cubes[i];
            var coord = new Vector2Int(
                Mathf.RoundToInt((positions[i].x - minX) / _cellSize),
                Mathf.RoundToInt((positions[i].z - minZ) / _cellSize));

            Renderer[] rends = root.GetComponentsInChildren<Renderer>();
            Bounds bound = CombinedBounds(rends, root.position);
            EnsureCollider(root, bound);

            var cell = new Cell
            {
                coord = coord,
                kind = Classify(root.name),
                root = root,
                renderers = rends,
                topY = bound.max.y
            };

            // 같은 칸에 큐브가 겹치면(바닥 위에 고지 큐브를 쌓은 경우) 더 높은 쪽을 대표로 삼는다.
            if (!_cells.TryGetValue(coord, out Cell existing) || cell.topY > existing.topY)
                _cells[coord] = cell;
            _byRoot[root] = cell;

            if (!hasBounds) { _worldBounds = bound; hasBounds = true; }
            else _worldBounds.Encapsulate(bound);
        }

        // 3) 높이 기반 고지 승격 + 스폰/본진 집계(이름 판정 뒤에 높이가 우선).
        Dictionary<TileKind, int> typeCount = FinalizeCells();

        Debug.Log($"[MapBoard] 큐브 {_cells.Count}개 (셀크기 {_cellSize:0.###}), " +
            $"지상 {Count(typeCount, TileKind.Ground)}, 고지 {Count(typeCount, TileKind.High)}, 경계 {Count(typeCount, TileKind.Border)}, " +
            $"스폰 {_spawns.Count}, 본진 {_cores.Count}, 미분류 {Count(typeCount, TileKind.Unknown)}", this);

        if (!HasEndpoints)
            Debug.LogWarning("[MapBoard] 스폰(Cube_Wood*)/본진(Cube_Stone*) 타일을 찾지 못했습니다. 큐브 이름을 확인하세요.", this);
    }    
    /// <summary>바닥보다 솟은 큐브를 High로 승격하고 스폰/본진 목록·집계를 채운다.</summary>
    private Dictionary<TileKind, int> FinalizeCells()
    {
        _spawns.Clear();
        _cores.Clear();

        float floorY = MedianTopY();
        float raise = _cellSize * highRaiseFactor;

        var typeCount = new Dictionary<TileKind, int>();
        foreach (Cell cell in _cells.Values)
        {
            // 이름과 무관하게 바닥보다 솟았으면 고지로 본다(경계 제외).
            if (detectHighByElevation && cell.kind != TileKind.Border && cell.topY - floorY > raise)
                cell.kind = TileKind.High;

            if (cell.kind == TileKind.Spawn) _spawns.Add(cell);
            else if (cell.kind == TileKind.Core) _cores.Add(cell);

            typeCount.TryGetValue(cell.kind, out int n);
            typeCount[cell.kind] = n + 1;
        }

        return typeCount;
    }

    /// <summary>바닥 기준 높이 = 전체 큐브 topY의 중앙값(다수인 바닥 타일 높이).</summary>
    private float MedianTopY()
    {
        var ys = new List<float>(_cells.Count);
        foreach (Cell c in _cells.Values) ys.Add(c.topY);
        ys.Sort();
        return ys.Count > 0 ? ys[ys.Count / 2] : 0f;
    }

    public List<Cell> FindEnemyPath()
    {
        if (!HasEndpoints) return null;

        List<Cell> path = Pathfinder.FindPath(
            _spawns, c => c.kind == TileKind.Core, WalkableNeighbors, HeuristicToNearestCore);
        if (path == null)
        {
            int reach = Pathfinder.ReachableCount(_spawns, WalkableNeighbors);
            Debug.LogWarning($"[MapBoard] 경로 없음.");
        }

        return path;
    }

    private IEnumerable<Cell> WalkableNeighbors(Cell cell)
    {
        foreach (Vector2Int dir in Dirs)
            if (_cells.TryGetValue(cell.coord + dir, out Cell nb) && nb.Walkable)
                yield return nb;
    }

    /// <summary>가장 가까운 본진까지의 맨해튼 거리(A* 휴리스틱). 4방향 균일비용에서 admissible → 최단 보장.</summary>
    private int HeuristicToNearestCore(Cell cell)
    {
        int best = int.MaxValue;
        foreach (Cell core in _cores)
        {
            int d = Mathf.Abs(cell.coord.x - core.coord.x) + Mathf.Abs(cell.coord.y - core.coord.y);
            if (d < best) best = d;
        }
        return best == int.MaxValue ? 0 : best;
    }

    private int WalkableCount()
    {
        int n = 0;
        foreach (Cell c in _cells.Values) if (c.Walkable) n++;
        return n;
    }

    public bool TryGetCell(Vector2Int coord, out Cell cell) => _cells.TryGetValue(coord, out cell);

    public Cell CellFromCollider(Collider col)
    {
        for (Transform t = col.transform; t != null; t = t.parent)
            if (_byRoot.TryGetValue(t, out Cell cell)) return cell;
        return null;
    }

    public bool IsPlaceable(Vector2Int coord) =>
        _cells.TryGetValue(coord, out Cell cell) && cell.IsEmpty
        && (cell.kind == TileKind.Ground || cell.kind == TileKind.High);

    /// <summary>기본 배치 규칙: 지상=근접유닛/건물, 고지=원거리만. placementRule로 덮어쓸 수 있다.</summary>
    private static bool DefaultAllowed(TileKind tile, PlaceKind kind) => tile switch
    {
        TileKind.Ground => kind == PlaceKind.Unit || kind == PlaceKind.Building,
        TileKind.High => kind == PlaceKind.Ranged,
        _ => false
    };

    public IEnumerable<Cell> GroundCells
    {
        get { foreach (Cell c in _cells.Values) if (c.kind == TileKind.Ground) yield return c; }
    }

    public List<Vector3> EnemyWaypoints(float yOffset = 0f)
    {
        var list = new List<Vector3>();
        List<Cell> path = FindEnemyPath();
        if (path != null)
            foreach (Cell c in path) list.Add(c.WorldTop + Vector3.up * yOffset);
        return list;
    }

    public bool CanPlace(Vector2Int coord, PlaceKind kind, out string reason)
    {
        reason = "";
        if (!_cells.TryGetValue(coord, out Cell cell)) { reason = "타일 없음"; return false; }
        if (!cell.IsEmpty) { reason = "이미 점유됨"; return false; }
        if (placementRule != null)
        {
            if (!placementRule(cell, kind)) { reason = "규칙상 배치 불가"; return false; }
            return true;
        }
        if (!DefaultAllowed(cell.kind, kind))
        {
            reason = cell.kind switch
            {
                TileKind.High => "고지엔 원거리 유닛만",
                TileKind.Ground => "지상엔 근접 유닛/건물만",
                _ => "배치 불가 지형"
            };
            return false;
        }
        return true;
    }

    public bool TryPlace(Vector2Int coord, GameObject go, PlaceKind kind, float yOffset, out string reason)
    {
        if (!CanPlace(coord, kind, out reason)) return false;

        Cell cell = _cells[coord];
        if (go != null)
        {
            go.transform.SetParent(cell.root, true);
            go.transform.position = cell.WorldTop + Vector3.up * yOffset;
        }
        cell.occupant = go;
        Occupied?.Invoke(cell);
        return true;
    }

    public GameObject Vacate(Vector2Int coord)
    {
        if (!_cells.TryGetValue(coord, out Cell cell) || cell.IsEmpty) return null;
        GameObject go = cell.occupant;
        cell.occupant = null;
        Vacated?.Invoke(cell);
        return go;
    }

    // ---- 공간 질의 (상호작용 틀) ----
    // 맵은 "몇 칸 이내에 무엇이 있나"만 계산해 후보를 돌려준다. 타겟 선정·공격·데미지는 담당 몫.

    /// <summary>월드 위치를 가장 가까운 칸 좌표로 변환(연속 이동하는 적의 현재 칸 파악용).</summary>
    public Vector2Int WorldToCell(Vector3 world) => new(
        Mathf.RoundToInt((world.x - _originX) / _cellSize),
        Mathf.RoundToInt((world.z - _originZ) / _cellSize));

    public static int TileDistance(Vector2Int a, Vector2Int b, bool chebyshev = false)
    {
        int dx = Mathf.Abs(a.x - b.x), dy = Mathf.Abs(a.y - b.y);
        return chebyshev ? Mathf.Max(dx, dy) : dx + dy;
    }

    public List<Cell> TilesInRange(Vector2Int origin, int range, bool square = false)
    {
        var result = new List<Cell>();
        for (int dx = -range; dx <= range; dx++)
            for (int dy = -range; dy <= range; dy++)
            {
                if (!square && Mathf.Abs(dx) + Mathf.Abs(dy) > range) continue;
                if (_cells.TryGetValue(new Vector2Int(origin.x + dx, origin.y + dy), out Cell cell))
                    result.Add(cell);
            }
        return result;
    }
    public List<GameObject> OccupantsInRange(Vector2Int origin, int range, bool square = false)
    {
        var list = new List<GameObject>();
        foreach (Cell c in TilesInRange(origin, range, square))
            if (c.occupant != null) list.Add(c.occupant);
        return list;
    }

    public List<IDamageAble> DamageablesInRange(Vector3 originWorld, int range, LayerMask mask, bool square = false)
    {
        var found = new HashSet<IDamageAble>();
        Vector2Int originCell = WorldToCell(originWorld);
        float radius = (range + 0.5f) * _cellSize; // 넉넉히 잡고 칸 거리로 정밀 필터

        foreach (Collider col in Physics.OverlapSphere(originWorld, radius, mask))
        {
            if (TileDistance(originCell, WorldToCell(col.transform.position), square) > range) continue;
            if (col.GetComponentInParent<IDamageAble>() is IDamageAble d) found.Add(d);
        }

        var list = new List<IDamageAble>(found);
        return list;
    }

    public void Highlight(Vector2Int coord, Color color)
    {
        if (_cells.TryGetValue(coord, out Cell cell)) Tint(cell, color);
    }

    public void ClearHighlight(Vector2Int coord)
    {
        if (_cells.TryGetValue(coord, out Cell cell)) Restore(cell);
    }

    private void Tint(Cell cell, Color color)
    {
        foreach (Renderer r in cell.renderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, color);
            r.SetPropertyBlock(_mpb);
        }
    }

    private void Restore(Cell cell)
    {
        foreach (Renderer r in cell.renderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.Clear();
            r.SetPropertyBlock(_mpb);
        }
    }

    private List<Transform> FindCubeRoots()
    {
        var roots = new List<Transform>();
        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if (t != null && t.name.StartsWith(cubePrefix)) roots.Add(t);
        return roots;
    }

    private TileKind Classify(string name)
    {
        foreach (string p in highCubePrefixes)
            if (!string.IsNullOrEmpty(p) && name.StartsWith(p)) return TileKind.High;
        if (name.StartsWith("Cube_Concrete")) return TileKind.Ground;
        if (name.StartsWith("Cube_Grass")) return TileKind.Border;
        if (name.StartsWith("Cube_Wood")) return TileKind.Spawn;
        if (name.StartsWith("Cube_Stone")) return TileKind.Core;
        return TileKind.Unknown;
    }

    
    private static float EstimateCellSize(List<Vector3> positions)
    {
        if (positions.Count < 2) return 1f;

        var nn = new List<float>(positions.Count);
        for (int i = 0; i < positions.Count; i++)
        {
            float best = float.MaxValue;
            for (int j = 0; j < positions.Count; j++)
            {
                if (i == j) continue;
                float dx = positions[i].x - positions[j].x;
                float dz = positions[i].z - positions[j].z;
                float d = dx * dx + dz * dz;
                if (d > 0.0001f && d < best) best = d;
            }
            if (best < float.MaxValue) nn.Add(Mathf.Sqrt(best));
        }

        if (nn.Count == 0) return 1f;
        nn.Sort();
        float median = nn[nn.Count / 2];
        return median > 0.01f ? median : 1f;
    }

    private static int Count(Dictionary<TileKind, int> counts, TileKind kind)
    {
        counts.TryGetValue(kind, out int n);
        return n;
    }

    private static Bounds CombinedBounds(Renderer[] rends, Vector3 fallback)
    {
        bool has = false;
        Bounds b = new(fallback, Vector3.one * 0.5f);
        foreach (Renderer r in rends)
        {
            if (r == null) continue;
            if (!has) { b = r.bounds; has = true; }
            else b.Encapsulate(r.bounds);
        }
        return b;
    }

    private static void EnsureCollider(Transform root, Bounds worldBounds)
    {
        if (root.GetComponentInChildren<Collider>() != null) return;

        var box = root.gameObject.AddComponent<BoxCollider>();
        box.center = root.InverseTransformPoint(worldBounds.center);
        Vector3 lossy = root.lossyScale;
        box.size = new Vector3(
            worldBounds.size.x / Mathf.Max(0.0001f, Mathf.Abs(lossy.x)),
            worldBounds.size.y / Mathf.Max(0.0001f, Mathf.Abs(lossy.y)),
            worldBounds.size.z / Mathf.Max(0.0001f, Mathf.Abs(lossy.z)));
    }
}
