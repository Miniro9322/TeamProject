using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 맵 세팅 자동화 도구. 씬의 <see cref="Tile"/> 타일들의 논리 좌표(Col/Row)를 위치에서 계산하고,
/// 경로(EnemyLane)와 초기 영토(Claimed)를 규칙으로 자동 결정한 뒤, 마지막에 검증 리포트를 낸다.
/// 목표: "프리팹으로 타일 칠하기 → Bake 한 번 → 검증 통과 확인"으로 맵을 세팅한다.
///
/// 지형·용도·스폰(Terrain/Flags/isEnemySpawn)은 이름으로 추론하지 않는다 — 각 큐브 프리팹이
/// Tile.State에 저작해 둔 값을 단일 소스로 신뢰한다(팀 지침: 이름 판별 금지).
/// 베이크는 좌표·경로·영토 같은 "파생 데이터"만 채운다.
///
/// 자동 결정 규칙:
///  - 좌표: 타일 위치를 셀크기로 정규화해 (Col,Row) 부여(런타임 MapBoard.Build와 동일 계산)
///  - 경로(EnemyLane): 스폰→본진 최단 경로를 계산해 그 타일들에 플래그(타일이 "나는 경로다"를 알게 함)
///  - 초기 영토(Claimed): 본진에서 맨해튼 N칸(MapBoard.initialClaimRadius) 이내의 지상/고지만 Claimed
/// </summary>
public static class MapTileBaker
{
    private const int DefaultClaimRadius = 4;
    private static readonly Vector2Int[] Dirs = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };

    [MenuItem("Tools/Map/Bake Tiles From Cubes (Active Scene)")]
    private static void BakeActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        List<Tile> tileArr = CollectTiles(scene);
        if (tileArr.Count == 0)
        {
            EditorUtility.DisplayDialog("Map Tile Baker",
                "Tile 컴포넌트를 가진 타일을 찾지 못했습니다. 큐브 프리팹에 Tile을 붙이고 " +
                "인스펙터에서 지형(State)을 저작한 뒤 배치하세요.", "확인");
            return;
        }

        // 좌표계 = 런타임 MapBoard.Build와 동일 계산(정합 보장).
        // 지형/용도/스폰은 프리팹이 저작한 State를 그대로 신뢰한다(이름 추론 없음).
        var positions = new List<Vector3>(tileArr.Count);
        foreach (Tile t in tileArr) positions.Add(t.transform.position);
        float cellSize = MapBoard.EstimateCellSize(positions);
        float minX = float.MaxValue, minZ = float.MaxValue;
        foreach (Vector3 p in positions) { minX = Mathf.Min(minX, p.x); minZ = Mathf.Min(minZ, p.z); }

        Undo.SetCurrentGroupName("Bake Tiles From Cubes");
        int group = Undo.GetCurrentGroup();

        // ── Phase 1: 논리 좌표(Col/Row)만 위치에서 계산. 지형/플래그/스폰은 저작값 유지 ──
        var tiles = new List<Tile>(tileArr.Count);
        foreach (Tile tile in tileArr)
        {
            Undo.RecordObject(tile, "Bake Tile");
            tile.State ??= new TileState();
            tile.State.ImportFlags();
            tile.State.Col = Mathf.RoundToInt((tile.transform.position.x - minX) / cellSize);
            tile.State.Row = Mathf.RoundToInt((tile.transform.position.z - minZ) / cellSize);
            tile.State.EnemyLane = false;
            tiles.Add(tile);
        }

        // ── Phase 2: 경로(EnemyLane) 자동 — 스폰→본진 최단경로 타일에 플래그 ──
        Dictionary<Vector2Int, Tile> byCoord = BuildCoordMap(tiles, out int duplicates);
        List<Tile> path = ComputePath(byCoord);
        if (path != null)
        {
            foreach (Tile pt in path)
            {
                if (!pt.isEnemySpawn && pt.Terrain != TerrainType.Core)
                {
                    pt.State.EnemyLane = true;
                }
            }
        }

        // ── Phase 3: 초기 영토(Claimed) — 본진에서 N칸 이내의 지상/고지 ──
        int radius = ClaimRadius();
        var cores = new List<Tile>();
        foreach (Tile t in tiles) if (t.Terrain == TerrainType.Core) cores.Add(t);
        int claimed = 0;
        foreach (Tile t in tiles)
        {
            bool placeableTerrain = t.Terrain is TerrainType.Ground or TerrainType.High && !t.isEnemySpawn;
            if (!placeableTerrain) { t.State.Territory = TerritoryState.Unclaimed; continue; }

            bool near = false;
            foreach (Tile c in cores)
                if (Manhattan(t, c) <= radius) { near = true; break; }
            t.State.Territory = near ? TerritoryState.Claimed : TerritoryState.Unclaimed;
            if (near) claimed++;
        }

        foreach (Tile t in tiles) EditorUtility.SetDirty(t);
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log($"[MapTileBaker] 베이크 완료 — Tile {tiles.Count}개, 셀크기 {cellSize:0.###}, " +
                  $"경로 {(path == null ? "실패" : path.Count + "칸")}, 영토(반경 {radius}) {claimed}칸.");
        Debug.Log(BuildReport(byCoord, path, cores, radius, duplicates));
    }

    [MenuItem("Tools/Map/Validate Active Scene")]
    private static void ValidateActiveScene()
    {
        List<Tile> tiles = CollectTiles(SceneManager.GetActiveScene());
        if (tiles.Count == 0)
        {
            EditorUtility.DisplayDialog("Map Validator",
                "Tile이 붙은 타일이 없습니다. 먼저 Bake를 실행하세요.", "확인");
            return;
        }

        var list = new List<Tile>(tiles);
        Dictionary<Vector2Int, Tile> byCoord = BuildCoordMap(list, out int duplicates);
        List<Tile> path = ComputePath(byCoord);
        var cores = new List<Tile>();
        foreach (Tile t in list) if (t.Terrain == TerrainType.Core) cores.Add(t);
        Debug.Log(BuildReport(byCoord, path, cores, ClaimRadius(), duplicates));
    }

    // ── 검증 리포트 ─────────────────────────────────────────────
    private static string BuildReport(Dictionary<Vector2Int, Tile> byCoord, List<Tile> path,
        List<Tile> cores, int radius, int duplicates)
    {
        int spawns = 0, melee = 0, ranged = 0, building = 0;
        foreach (Tile t in byCoord.Values)
        {
            if (t.isEnemySpawn) spawns++;
            if (t.State.Territory != TerritoryState.Claimed) continue;
            t.State.ImportFlags();

            if (t.State.CanMelee)
            {
                melee++;
            }

            if (t.State.CanRanged)
            {
                ranged++;
            }

            if (t.State.CanBuild)
            {
                building++;
            }
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[MapValidator] 검증 리포트");
        sb.AppendLine(Line(cores.Count > 0, $"본진(Core) {cores.Count}개"));
        sb.AppendLine(Line(spawns > 0, $"스폰 {spawns}개"));
        sb.AppendLine(Line(path != null, path != null ? $"스폰→본진 경로 연결됨 ({path.Count}칸)" : "스폰→본진 경로 없음 — 통행 가능 지형이 끊겼는지 확인"));
        sb.AppendLine(Line(building > 0, $"초기 영토 내 건물 가능 {building}칸", warnIfZero: true));
        sb.AppendLine(Line(melee > 0, $"초기 영토 내 근접 배치 {melee}칸", warnIfZero: true));
        sb.AppendLine(Line(ranged > 0, $"초기 영토 내 원거리 배치 {ranged}칸", warnIfZero: true));
        if (duplicates > 0)
            sb.AppendLine($"  WARN: 같은 좌표에 겹친 타일 {duplicates}개 — 큐브 정렬/셀크기 확인(더 높은 쪽만 격자에 남음)");
        return sb.ToString().TrimEnd();
    }

    private static string Line(bool ok, string msg, bool warnIfZero = false)
        => ok ? $"  OK: {msg}" : $"  {(warnIfZero ? "WARN" : "FAIL")}: {msg}";

    // ── 경로 계산 (런타임 MapBoard와 동일: 통행=Ground 또는 Core, 목표=Core) ──
    private static List<Tile> ComputePath(Dictionary<Vector2Int, Tile> byCoord)
    {
        var spawns = new List<Tile>();
        var cores = new List<Tile>();
        foreach (Tile t in byCoord.Values)
        {
            if (t.isEnemySpawn) spawns.Add(t);
            if (t.Terrain == TerrainType.Core) cores.Add(t);
        }
        if (spawns.Count == 0 || cores.Count == 0) return null;

        return Pathfinder.FindPath(
            spawns,
            t => t.Terrain == TerrainType.Core,
            t => Neighbors(byCoord, t),
            t => NearestCore(cores, t));
    }

    private static IEnumerable<Tile> Neighbors(Dictionary<Vector2Int, Tile> byCoord, Tile tile)
    {
        foreach (Vector2Int dir in Dirs)
            if (byCoord.TryGetValue(tile.Coord + dir, out Tile nb) && nb.Walkable)
                yield return nb;
    }

    private static int NearestCore(List<Tile> cores, Tile tile)
    {
        int best = int.MaxValue;
        foreach (Tile c in cores) best = Mathf.Min(best, Manhattan(tile, c));
        return best == int.MaxValue ? 0 : best;
    }

    private static int Manhattan(Tile a, Tile b)
        => Mathf.Abs(a.State.Col - b.State.Col) + Mathf.Abs(a.State.Row - b.State.Row);

    // ── 헬퍼 ─────────────────────────────────────────────────────
    private static Dictionary<Vector2Int, Tile> BuildCoordMap(List<Tile> tiles, out int duplicates)
    {
        var byCoord = new Dictionary<Vector2Int, Tile>();
        duplicates = 0;
        foreach (Tile t in tiles)
        {
            Vector2Int c = t.Coord;
            if (byCoord.TryGetValue(c, out Tile existing))
            {
                duplicates++;
                // 더 높은(솟은) 쪽을 대표로 — MapBoard.Build와 동일 규칙.
                if (t.transform.position.y <= existing.transform.position.y) continue;
            }
            byCoord[c] = t;
        }
        return byCoord;
    }

    private static int ClaimRadius()
    {
        MapBoard board = BoardInScene();
        return board != null ? board.initialClaimRadius : DefaultClaimRadius;
    }

    // 씬 순회로 타일/보드를 모은다(Find 함수 미사용 — GetRootGameObjects + GetComponentsInChildren).
    private static List<Tile> CollectTiles(Scene scene)
    {
        var tiles = new List<Tile>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            tiles.AddRange(root.GetComponentsInChildren<Tile>(true));
        }
        return tiles;
    }

    private static MapBoard BoardInScene()
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            MapBoard board = root.GetComponentInChildren<MapBoard>(true);
            if (board != null)
            {
                return board;
            }
        }
        return null;
    }
}
