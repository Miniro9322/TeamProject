using UnityEngine;

// 집어 든 유닛 하나에 딸린 값 묶음. 아무것도 하지 않고 들고만 있는다.
// 다섯 값이 함께 채워지고 함께 비워진다 — 반만 남은 상태를 만들지 않으려고 묶었다.
// 값이라 빈 상태(Unit이 null)도 만들어진다. 그게 "아무것도 안 집은 상태"다.
public readonly struct HeldData
{
    // 보드에서 뗀 채 포인터를 따라가는 유닛.
    public readonly GameObject Unit;

    // 집은 출발 칸. 제자리에 도로 놓았는지 가릴 때 쓴다.
    public readonly Tile FromTile;

    public readonly OccupantKind Kind;

    // 사거리 커버를 다시 깔 때 쓴다.
    public readonly int Range;

    // 집기 전에 덮고 있던 칸 수. 내려놓을 때 같은 크기로 돌아간다.
    public readonly Vector2Int Size;

    public HeldData(
        GameObject unit,
        Tile fromTile,
        OccupantKind kind,
        int range,
        Vector2Int size)
    {
        Unit = unit;
        FromTile = fromTile;
        Kind = kind;
        Range = range;
        Size = size;
    }
}
