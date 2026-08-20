// 타이틀에서 고른 슬롯 번호와 새 게임 여부 
 
public static class SelectedSaveSlot
{
    public static int SlotId { get; private set; } = 1;
    public static bool IsNewGame { get; private set; } = true;

    // 새 게임으로 진입할 슬롯을 지정한다.
    public static void SetNewGame(int slotId)
    {
        SlotId = slotId;
        IsNewGame = true;
    }

    // 불러오기로 진입할 슬롯을 지정한다.
    public static void SetLoad(int slotId)
    {
        SlotId = slotId;
        IsNewGame = false;
    }
}

