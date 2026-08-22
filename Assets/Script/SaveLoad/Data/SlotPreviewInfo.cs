
// 슬롯 한 칸의 미리보기 정보.
public readonly struct SlotPreviewInfo
{
    public static readonly SlotPreviewInfo Empty = new SlotPreviewInfo(false, 0, 0f, string.Empty, 0);

    public readonly bool HasSave;
    public readonly int DayCount;
    public readonly float PlayTime;
    public readonly string SaveTime;
    public readonly int HeroCount;

    public SlotPreviewInfo(bool hasSave, int dayCount, float playTime, string saveTime, int heroCount)
    {
        HasSave = hasSave;
        DayCount = dayCount;
        PlayTime = playTime;
        SaveTime = saveTime;
        HeroCount = heroCount;
    }
}
