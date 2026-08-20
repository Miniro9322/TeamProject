
// 슬롯 한 칸의 미리보기 정보.
public readonly struct SlotPreviewInfo
{
    public readonly bool HasSave;
    public readonly int DayCount;
    public readonly float PlayTime;
    public readonly string SaveTime;

    public SlotPreviewInfo(bool hasSave, int dayCount, float playTime, string saveTime)
    {
        HasSave = hasSave;
        DayCount = dayCount;
        PlayTime = playTime;
        SaveTime = saveTime;
    }
}
