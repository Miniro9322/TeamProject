using System;


[Serializable]
public class TileState
{
    // 논리 좌표. 화면 좌표가 아니라 정사각 그리드 (col, row) 정수(설계 §2).
    public int Col;
    public int Row;

    public TerrainType Terrain = TerrainType.Ground;
    public TerritoryState Territory = TerritoryState.Unclaimed;

    public bool EnemyLane;
    public bool CanMelee;
    public bool CanRanged;
    public bool CanBuild;

    // 기존 씬 데이터 호환용. 새 판정 코드는 위 명시 필드만 사용한다.
    public TileFlags Flags = TileFlags.None;

    // 이 타일의 주요 점유 종류(설계 §3 Occupants·§4 "한 타일 하나의 주요 점유").
    // 적 경로는 점유가 아니라 EnemyLane으로 관리하므로 여기 넣지 않는다.
    // 실제 점유 오브젝트(런타임 GameObject)는 Tile.OccupantObject가 들고, 여기엔 "종류"만 둔다.
    public OccupantKind Occupant = OccupantKind.None;

    public TileState() { }

    public TileState(int col, int row)
    {
        Col = col;
        Row = row;
    }

    public bool IsEmpty => Occupant == OccupantKind.None;

    public string Label => $"{(char)('a' + Col)}{Row + 1}";

    public void ImportFlags()
    {
        int value = (int)Flags;
        if (value == 0)
        {
            return;
        }

        if (HasOld(value, (int)TileFlags.EnemyLane))
        {
            EnemyLane = true;
        }

        if (HasOld(value, (int)TileFlags.MeleePlaceable))
        {
            CanMelee = true;
        }

        if (HasOld(value, (int)TileFlags.RangedPlaceable))
        {
            CanRanged = true;
        }

        if (HasOld(value, (int)TileFlags.BuildingPlaceable))
        {
            CanBuild = true;
        }
    }

    private static bool HasOld(int value, int flag)
    {
        if (flag <= 0)
        {
            return false;
        }

        return value / flag % 2 == 1;
    }

    public override string ToString() => $"{Label} ({Col},{Row}) {Terrain}/{Territory}";
}

/// <summary>타일을 점유하는 주요 오브젝트 종류. 한 타일에 하나만(설계 §4).</summary>
public enum OccupantKind
{
    None,
    MeleeHero,  // 근접 영웅
    RangedHero, // 원거리 영웅
    Building,    // 생산 건물
    Resource,   // 자원(설계 §4.3.1)

}
