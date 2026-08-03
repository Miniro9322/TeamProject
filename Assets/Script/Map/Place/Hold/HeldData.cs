using UnityEngine;

// 집어 든 유닛 하나와 그 유닛에 딸린 값들.
public readonly struct HeldData
{
    // 판에서 뗀 채 포인터를 따라가는 유닛.
    public readonly GameObject Unit;

    // 집어 든 출발 칸.
    public readonly Tile FromTile;

    public readonly OccupantKind Kind;

    // 덮고 있던 칸 수.
    public readonly Vector2Int Size;

    public HeldData(
        GameObject unit,
        Tile fromTile,
        OccupantKind kind,
        Vector2Int size)
    {
        Unit = unit;
        FromTile = fromTile;
        Kind = kind;
        Size = size;
    }
}
