 // TempTitle 씬에서 DI 없이 슬롯 파일을 직접 읽어 SlotPreviewInfo로 바꾼다
public class SlotPreviewReader
{
    private readonly SaveSlot saveSlot;

 
    public SlotPreviewReader()
    {
        SaveCheck saveCheck = new SaveCheck();
        SaveIO saveIO = new SaveIO(saveCheck);
        saveSlot = new SaveSlot(saveIO, saveCheck);
    }

    // 지정한 슬롯의 저장 여부와 미리보기 정보를 읽는다.
    public SlotPreviewInfo ReadSaveSlot(int slotId)
    {
        if (!saveSlot.TryReadLatest(slotId, out SaveFile file))
        {
            return new SlotPreviewInfo(false, 0, 0f, string.Empty, 0);
        }

        return new SlotPreviewInfo(true, file.saveData.dayCount, file.saveData.playTime, file.saveData.saveTime, file.saveData.heroList.Length);
    }

    // 슬롯이 1개라도 저장되어 있는지 확인한다 
    public bool HasAnySave()
    {
        for (int slotId = 1; slotId <= SaveSlotConfig.SlotCount; slotId++)
        {
            if (ReadSaveSlot(slotId).HasSave) return true;
        }
        return false;
    }
}
