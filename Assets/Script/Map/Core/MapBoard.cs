using System.Collections.Generic;
using UnityEngine;

public class MapBoard : MonoBehaviour
{
    private readonly Dictionary<Vector2Int, Tile> _cells = new();
    private readonly List<Tile> _spawns = new();
    private readonly List<Tile> _cores = new();
    private readonly Dictionary<GameObject, Tile> _enemyCell = new(); // 적→현재 칸(직전 칸과 비교해 이동 감지)

    [SerializeField] private Grid _grid; // 좌표계의 단일 소스. 셀 크기·원점·Swizzle을 모두 쥔다.
    private Bounds _worldBounds;
    private RectInt _playRect;
    private float _floorY; // 클릭 판정에서 타일 기둥이 내려가는 바닥(보드 기준 높이).
    private ModuleLogic _module; // 소속 모듈. Awake에서 한 번만 잡는다(매 호출 GetComponent 금지).

    public IReadOnlyDictionary<Vector2Int, Tile> Cells => _cells;
    public int CellCount => _cells.Count;
    public Bounds WorldBounds => _worldBounds;
    public float CellSize => _grid.cellSize.x;

    // 외곽 장식 줄을 뺀 맵 안쪽 칸 범위. 배치물이 맵 밖으로 삐져나가지 않게 맞추는 기준.
    public RectInt PlayRect => _playRect;

    private bool HasEndpoints => _spawns.Count > 0 && _cores.Count > 0;

    // 이 보드의 모듈이 해금됐는가(잠긴 모듈에는 아무것도 놓을 수 없다).
    public bool IsUnlocked => _module.IsUnlocked;
 

    private void Awake()
    {
        _module = GetComponent<ModuleLogic>();
        Build();
    }

    [ContextMenu("Build")]
    public void Build()
    {
        _cells.Clear();
        _spawns.Clear();
        _cores.Clear();
        _enemyCell.Clear();

        // 이 모듈 Grid 하위 타일만 모은다(씬 전체 스캔 금지 — 모듈 격리). Find 미사용.
        var tiles = new List<Tile>();
        tiles.AddRange(_grid.GetComponentsInChildren<Tile>(true));

        // 1) 타일마다 렌더 캐시 + 격자 등록. 논리 좌표는 각 타일의 State.Col/Row를 신뢰한다(베이크가 새김).
        bool hasBounds = false;
        foreach (Tile tile in tiles)
        {
            tile.SetBoard(this); // 소유 보드 도장 — 이후 모든 소비자는 tile.Board로 자기 모듈 보드를 찾는다
            tile.State.ImportFlags();

            Transform root = tile.transform;
            Renderer[] rends = root.GetComponentsInChildren<Renderer>();
            Bounds bound = CombinedBounds(rends, root.position);
            tile.SetTop(bound.max.y); // 윗면 높이만 Core에 캐시(렌더러/색은 Core가 모른다)

            Vector2Int coord = tile.Coord;
            // 같은 칸에 타일이 겹치면(바닥 위에 고지 큐브를 쌓은 경우) 더 높은 쪽을 대표로 삼는다.
            if (!_cells.TryGetValue(coord, out Tile existing) || tile.WorldTop.y > existing.WorldTop.y)
                _cells[coord] = tile;

            if (tile.isEnemySpawn) _spawns.Add(tile);
            if (tile.Terrain == TerrainType.Core) _cores.Add(tile);

            if (!hasBounds) { _worldBounds = bound; hasBounds = true; }
            else _worldBounds.Encapsulate(bound);
            
        }

        // 2) 안쪽 칸 범위: 장식(Special)을 뺀 타일들의 바운딩 박스. 외곽 한 줄이 장식이라 그만큼 좁다.
        _playRect = InnerRect();
        _floorY = LowestFloor();

        Debug.Log($"[MapBoard] 타일 {_cells.Count}개 (안쪽 {_playRect.width}×{_playRect.height} @{_playRect.min}, " +
            $"셀크기 {CellSize:0.###}), 스폰 {_spawns.Count}, 본진 {_cores.Count}", this);

        if (!HasEndpoints)
            Debug.LogWarning("[MapBoard] 스폰(isEnemySpawn) 또는 본진(Terrain=Core) 타일을 찾지 못했습니다.", this);
    }

    // 클릭 판정용 기둥의 바닥. 가장 낮은 타일 윗면보다 한 칸 더 내려간 높이라 어떤 타일도 두께를 가진다.
    // 월드 바운즈 대신 보드 기준으로 재는 이유는 맵을 돌려 놓으면 월드 높이가 실제 아래쪽과 달라지기 때문이다.
    private float LowestFloor()
    {
        Transform space = _grid.transform;
        float lowest = float.MaxValue;

        foreach (Tile tile in _cells.Values)
        {
            float top = space.InverseTransformPoint(tile.WorldTop).y;
            if (top < lowest) { lowest = top; }
        }

        return lowest == float.MaxValue ? 0f : lowest - CellSize;
    }

    // 장식이 아닌 타일들을 감싸는 직사각형. 장식 줄이 사방 한 줄이라 전체보다 한 칸씩 안쪽으로 들어온다.
    private RectInt InnerRect()
    {
        Vector2Int min = new(int.MaxValue, int.MaxValue);
        Vector2Int max = new(int.MinValue, int.MinValue);

        foreach (Tile tile in _cells.Values)
        {
            if (tile.IsSpecial) { continue; }

            min = Vector2Int.Min(min, tile.Coord);
            max = Vector2Int.Max(max, tile.Coord);
        }

        if (max.x < min.x)
        {
            return new RectInt();   // 장식뿐인 판 — 맞출 기준이 없으니 배치 판정에 맡긴다
        }

        return new RectInt(min.x, min.y, max.x - min.x + 1, max.y - min.y + 1);
    }

    // ---- 경로 ----

    private List<Tile> GetPath()
    {
        if (!HasEndpoints)
        {
            ClearLanes();
            return null;
        }

        List<Tile> path = Pathfinder.FindPath(
            _spawns, t => t.Terrain == TerrainType.Core, WalkableNeighbors, HeuristicToNearestCore);
        SetLanes(path);

        return path;
    }

    private void SetLanes(List<Tile> path)
    {
        ClearLanes();
        foreach (Tile tile in path)
        {
            if (tile.IsEnemySpawn) continue; 
            if (tile.IsCore) continue;
            tile.State.EnemyLane = true;
        }
    }

    private void ClearLanes()
    {
        foreach (Tile tile in _cells.Values)
        {
            tile.State.EnemyLane = false;
        }
    }

    private IEnumerable<Tile> WalkableNeighbors(Tile tile)
    {
        foreach (Vector2Int dir in GridCalculator.Directions)
            if (_cells.TryGetValue(tile.Coord + dir, out Tile nb) && nb.Walkable)
                yield return nb;
    }

    // 가장 가까운 본진까지의 칸 거리. 경로 찾기가 어느 쪽을 먼저 뒤질지 정하는 데 쓴다.
    private int HeuristicToNearestCore(Tile tile)
    {
        int best = int.MaxValue;
        foreach (Tile core in _cores)
        {
            int d = GridCalculator.GetDistance(tile.Coord, core.Coord);
            if (d < best) best = d;
        }
        return best == int.MaxValue ? 0 : best;
    }

    public List<Vector3> GetWaypoints(float yOffset = 0f)
    {
        var list = new List<Vector3>();
        List<Tile> path = GetPath();
        foreach (Tile t in path) 
        {
            list.Add(t.WorldTop + Vector3.up * yOffset);
        }
        return list;
    }

    // ---- 조회 ----

    public bool TryGetCell(Vector2Int coord, out Tile tile) => _cells.TryGetValue(coord, out tile);

    // 화면 광선이 가리키는 타일. 타일을 윗면 한 장이 아니라 바닥까지 이어진 기둥으로 보고 맞힌다
    // — 그래서 옆면을 눌러도 그 타일이 잡히고, 앞에 선 높은 타일이 뒤 타일을 가린다.
    public Tile CellFromRay(Ray ray)
    {
        Transform space = _grid.transform; // 맵을 돌려 놔도 축이 어긋나지 않게 보드 기준으로 옮겨서 잰다
        var local = new Ray(
            space.InverseTransformPoint(ray.origin),
            space.InverseTransformDirection(ray.direction));

        float half = CellSize * 0.5f;
        Tile best = null;
        float bestT = float.MaxValue;

        foreach (Tile tile in _cells.Values)
        {
            Vector3 top = space.InverseTransformPoint(tile.WorldTop);
            if (!TryEnterColumn(local, top, half, _floorY, out float t)) continue;
            if (t >= bestT) continue; // 이미 더 앞에서 맞은 타일이 있으면 그쪽이 이 타일을 가린다

            bestT = t;
            best = tile;
        }
        return best;
    }

    // 광선이 기둥(칸 사각형 × 바닥~윗면)으로 들어오는 지점. 스치지도 않으면 false.
    private static bool TryEnterColumn(Ray ray, Vector3 top, float half, float floorY, out float t)
    {
        t = 0f;
        float enter = 0f;
        float exit = float.MaxValue;

        if (!Narrow(ray.origin.x, ray.direction.x, top.x - half, top.x + half, ref enter, ref exit)) return false;
        if (!Narrow(ray.origin.z, ray.direction.z, top.z - half, top.z + half, ref enter, ref exit)) return false;
        if (!Narrow(ray.origin.y, ray.direction.y, floorY, top.y, ref enter, ref exit)) return false;

        t = enter;
        return true;
    }

    // 한 축에서 광선이 [min,max] 안에 머무는 구간만 남긴다. 세 축이 모두 남으면 기둥을 통과한 것.
    private static bool Narrow(float origin, float direction, float min, float max, ref float enter, ref float exit)
    {
        if (Mathf.Abs(direction) < 1e-6f)
        {
            return origin >= min && origin <= max; // 그 축으로 안 움직이면 처음부터 안에 있어야 한다
        }

        float near = (min - origin) / direction;
        float far = (max - origin) / direction;
        if (near > far) { (near, far) = (far, near); }

        enter = Mathf.Max(enter, near);
        exit = Mathf.Min(exit, far);
        return enter <= exit;
    }

    // 광선 아래 타일, 없으면 맵 밖이라도 가장 가까운 타일. 집은 유닛이 가장자리에 붙어 따라오게 한다.
    public Tile NearestCellFromRay(Ray ray)
    {
        Tile hit = CellFromRay(ray);
        if (hit != null) return hit;
        if (_cells.Count == 0 || Mathf.Abs(ray.direction.y) < 1e-6f) return null;

        float t = (_worldBounds.center.y - ray.origin.y) / ray.direction.y;
        if (t < 0f) return null; // 카메라 뒤쪽이면 클램프 안 함
        Vector3 p = ray.origin + ray.direction * t;

        Tile best = null;
        float bestSqr = float.MaxValue;
        foreach (Tile tile in _cells.Values)
        {
            Vector3 top = tile.WorldTop;
            float dx = p.x - top.x, dz = p.z - top.z;
            float sqr = dx * dx + dz * dz;
            if (sqr < bestSqr) { bestSqr = sqr; best = tile; }
        }
        return best;
    }

    // ---- 배치 ----

    public bool CanPlace(Vector2Int coord, OccupantKind kind)
    {
        return _cells.TryGetValue(coord, out Tile tile) 
            && TilePlacementRule.CanPlace(tile.State, kind);
    }

    // ---- 적 격자 점유 (움직이는 적의 현재 칸 추적 — 좌표 기반) ----
    // 적은 타일 점유(OccupantObject)와 별개다: 한 칸에 여러 마리가 드나들 수 있다.
    // 적 이동 컴포넌트가 월드 위치를 알려주면, 칸이 바뀐 경우에만 이전/새 타일을 갱신한다.

    // 적이 제 월드 위치를 알려오면 지금 서 있는 칸을 다시 잡는다.
    public void MoveEnemy(GameObject enemy, Vector3 world)
    {
        if (enemy == null) return;
        _cells.TryGetValue(WorldToCell(world), out Tile now); // 격자 밖이면 now = null
        ApplyEnemyCell(enemy, now);
    }

    // enter/exit 단일 처리 — 현재 칸(now, null=격자 밖)과 직전 칸을 비교해 바뀐 경우에만 갱신한다.
    // 4→5 이동 시: 4(prev)에서 RemoveEnemy, 5(now)에 AddEnemy. 안 바뀌면 값싸게 return.
    private void ApplyEnemyCell(GameObject enemy, Tile now)
    {
        _enemyCell.TryGetValue(enemy, out Tile prev);
        if (prev == now) return;

        if (prev != null) { prev.RemoveEnemy(enemy); }
        if (now != null)
        {
            now.AddEnemy(enemy);
            _enemyCell[enemy] = now;
        }
        else
        {
            _enemyCell.Remove(enemy);
        }
    }

    // 적이 사라질 때 서 있던 칸에서 지운다.
    public void RemoveEnemy(GameObject enemy)
    {
        if (!_enemyCell.TryGetValue(enemy, out Tile tile)) return;
        _enemyCell.Remove(enemy);
        if (tile != null) { tile.RemoveEnemy(enemy); }
    }

    public bool IsBlocked(GameObject enemy)
        => _enemyCell.TryGetValue(enemy, out Tile tile) && BlockCalc.IsBlocked(tile, enemy);

     

    // 월드 위치가 어느 칸인지. 변환은 Grid가 하므로 타일에 새겨진 좌표와 항상 같은 기준이다.
    public Vector2Int WorldToCell(Vector3 world)
    {
        Vector3Int cell = _grid.WorldToCell(world);
        return new Vector2Int(cell.x, cell.y);
    }

    // 월드 지점이 칸 안 어디인지까지 소수점으로. 칸 모서리가 정수, 한가운데가 x.5다.
    public Vector2 WorldToCellPoint(Vector3 world)
    {
        Vector3 cell = _grid.LocalToCellInterpolated(_grid.WorldToLocal(world));
        return new Vector2(cell.x, cell.y);
    }

    // 소수점 칸 좌표가 가리키는 월드 지점. 높이는 쓰는 쪽에서 갈아 끼운다.
    public Vector3 CellPointToWorld(Vector2 point)
    {
        return _grid.LocalToWorld(_grid.CellToLocalInterpolated(new Vector3(point.x, point.y, 0f)));
    }

    public List<Tile> GetTiles(Vector2Int origin, int range, bool square = false)
    {
        var result = new List<Tile>();
        for (int dx = -range; dx <= range; dx++)
            for (int dy = -range; dy <= range; dy++)
            {
                if (!square && Mathf.Abs(dx) + Mathf.Abs(dy) > range) continue;
                if (_cells.TryGetValue(new Vector2Int(origin.x + dx, origin.y + dy), out Tile tile))
                    result.Add(tile);
            }
        return result;
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
}
