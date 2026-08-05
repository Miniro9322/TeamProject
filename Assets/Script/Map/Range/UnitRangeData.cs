// 지금 보여줄 유닛의 사거리 값과 모양을 들고 있다가 내준다. 계산하거나 표시하지는 않는다.
public class UnitRangeData
{
    private int range;
    private RangeShape shape = RangeShape.Diamond;

    public int Range => range;

    public RangeShape Shape => shape;

    public void KeepRange(int unitRange, RangeShape unitShape)
    {
        range = unitRange;
        shape = unitShape;
    }

    public void ClearRange()
    {
        range = 0;
        shape = RangeShape.Diamond;
    }
}
