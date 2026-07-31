// 한 번 놓는 데 필요한 값 묶음. 아무것도 하지 않고 들고만 있는다.
// 미리보기와 확정이 같은 값을 읽는다 — 각자 계산하면 둘이 어긋난다.
// 값이라 빈 상태(Area가 null)도 만들어진다. PlaceFinder가 true를 준 것만 쓴다.
public readonly struct PlaceData
{
    // 덮는 칸들.
    public readonly PlacementArea Area;

    // 유닛이 설 높이.
    public readonly float TopY;

    // 지형·점유 판정. 자원·낮밤·로스터 게이트는 담지 않는다.
    public readonly bool CanPlace;

    public PlaceData(
        PlacementArea area,
        float topY,
        bool canPlace)
    {
        Area = area;
        TopY = topY;
        CanPlace = canPlace;
    }
}
