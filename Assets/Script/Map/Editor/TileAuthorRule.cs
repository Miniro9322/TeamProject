using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 저작된 타일의 문제를 짚어내는 에디터 전용 도구.
/// 새 판정을 만들지 않고 게임이 실제로 쓰는 TilePlacementRule에 그대로 물어본다 —
/// 여기서 "놓을 수 있다"고 나오면 런타임에서도 놓을 수 있다.
/// </summary>
public static class TileAuthorRule
{
    /// <summary>
    /// 지형은 멀쩡한데 배치 플래그가 없어서 아무것도 못 놓는 칸인가.
    /// 지상은 CanMelee·CanBuild 중 하나도 없을 때, 고지는 CanRanged가 없을 때 해당한다.
    /// 본진·특수·빈 타일은 원래 배치 불가라 문제로 보지 않는다.
    /// 스폰 칸도 빼둔다 — 적이 튀어나오는 자리를 비워두는 것은 실수가 아니라 저작 의도다.
    /// </summary>
    public static bool IsDeadCell(Tile tile)
    {
        if (tile.IsEnemySpawn)
        {
            return false;
        }

        TileState state = EffectiveState(tile);

        if (state.Terrain == TerrainType.Ground)
        {
            bool melee = TilePlacementRule.CanPlace(state, OccupantKind.MeleeHero).Allowed;
            bool build = TilePlacementRule.CanPlace(state, OccupantKind.Building).Allowed;
            return !melee && !build;
        }

        if (state.Terrain == TerrainType.High)
        {
            return !TilePlacementRule.CanPlace(state, OccupantKind.RangedHero).Allowed;
        }

        return false;
    }

    /// <summary>
    /// 런타임이 실제로 판정에 쓰는 상태. 옛 씬 데이터는 배치 허용을 명시 필드가 아니라 Flags 비트에
    /// 담고 있고, MapBoard.Build가 ImportFlags로 그걸 합친 뒤에야 배치가 된다.
    /// 감사도 같은 값을 봐야 하므로 여기서 사본에 합친다 — 원본 타일은 건드리지 않는다.
    /// </summary>
    private static TileState EffectiveState(Tile tile)
    {
        TileState source = tile.State;
        var copy = new TileState(source.Col, source.Row)
        {
            Terrain = source.Terrain,
            EnemyLane = source.EnemyLane,
            CanMelee = source.CanMelee,
            CanRanged = source.CanRanged,
            CanBuild = source.CanBuild,
            Flags = source.Flags,
            Occupant = source.Occupant
        };

        copy.ImportFlags();
        return copy;
    }

    /// <summary>이 모듈에서 지금 잡히는 문제들. 없으면 빈 목록.</summary>
    public static List<string> FindProblems(
        Dictionary<Vector2Int, Tile> cells,
        IReadOnlyList<LaneData> lanes,
        int tileCount)
    {
        var problems = new List<string>();

        if (cells.Count == 1 && tileCount > 1)
        {
            problems.Add($"타일 {tileCount}개가 전부 (0,0)에 모여 있습니다 — 좌표가 아직 안 새겨졌습니다. " +
                "Tools/Map/Bake Tile Positions (Active Scene)를 먼저 실행하세요.");
        }

        int spawnCount = LaneQuery.CollectSpawns(cells).Count;
        int coreCount = LaneQuery.CollectCores(cells).Count;

        if (spawnCount == 0)
        {
            problems.Add("적 스폰이 없습니다 — Spawn 붓으로 시작 칸을 찍으세요.");
        }

        if (coreCount == 0)
        {
            problems.Add("본진(Core)이 없습니다 — Core 붓으로 도착 칸을 찍으세요.");
        }

        if (spawnCount > 0 && coreCount > 0)
        {
            AddLaneProblems(lanes, problems);
        }

        AddDeadCells(cells, problems);
        return problems;
    }

    private static void AddLaneProblems(
        IReadOnlyList<LaneData> lanes,
        List<string> problems)
    {
        for (int i = 0; i < lanes.Count; i++)
        {
            LaneData lane = lanes[i];
            if (lane.IsValid)
            {
                continue;
            }

            problems.Add($"스폰 {lane.Start.Coord}에서 본진까지 가는 길이 없습니다 — " +
                "High나 Empty가 통로를 완전히 막았습니다.");
        }
    }

    // 배치 플래그가 비어 못 쓰는 칸을 지형별로 세어 알린다. 인스펙터로는 눈에 안 띄는 실수다.
    private static void AddDeadCells(Dictionary<Vector2Int, Tile> cells, List<string> problems)
    {
        var deadGround = new List<string>();
        var deadHigh = new List<string>();

        foreach (Tile tile in cells.Values)
        {
            if (!IsDeadCell(tile))
            {
                continue;
            }

            if (tile.Terrain == TerrainType.Ground)
            {
                deadGround.Add(tile.State.Label);
            }
            else
            {
                deadHigh.Add(tile.State.Label);
            }
        }

        if (deadGround.Count > 0)
        {
            problems.Add($"지상인데 근접·건물 둘 다 못 놓는 칸 {deadGround.Count}개 " +
                $"({Preview(deadGround)}) — CanMelee 또는 CanBuild 붓을 찍으세요.");
        }

        if (deadHigh.Count > 0)
        {
            problems.Add($"고지인데 원거리를 못 놓는 칸 {deadHigh.Count}개 " +
                $"({Preview(deadHigh)}) — CanRanged 붓을 찍으세요.");
        }
    }

    // 칸 이름을 최대 6개까지만 보여준다 — 목록이 길어지면 창이 문제 설명보다 좌표로 덮인다.
    private static string Preview(List<string> labels)
    {
        int shown = Mathf.Min(labels.Count, 6);
        string joined = string.Join(", ", labels.GetRange(0, shown));

        if (labels.Count > shown)
        {
            return joined + " …";
        }

        return joined;
    }
}
