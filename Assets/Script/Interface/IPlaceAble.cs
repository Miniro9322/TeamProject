interface IPlaceAble
{
    MapBoard Board { get; }
    void SetBoard(MapBoard board);
    event System.Action OnBreak;
    event System.Action OnResur;
}
