using Cysharp.Threading.Tasks;
using VContainer.Unity;

// 씬이 켜질 때 저장된 슬롯을 읽어 게임 상태에 되돌린다.
public class LoadManager : IStartable
{
    private readonly SaveSlot saveSlot;
    private readonly SaveRestore saveRestore;
    private readonly DayNightButton dayNightButton;
    private readonly SaveTimeData saveTimeData;

    // 로드에 필요한 저장 슬롯·복원기·버튼·시간 데이터를 받아 둔다.
    public LoadManager(
        SaveSlot saveSlot,
        SaveRestore saveRestore,
        DayNightButton dayNightButton,
        SaveTimeData saveTimeData)
    {
        this.saveSlot = saveSlot;
        this.saveRestore = saveRestore;
        this.dayNightButton = dayNightButton;
        this.saveTimeData = saveTimeData;
    }

    // 모든 Start()가 끝난 다음 프레임에 한 번만 로드한다.
    public void Start()
    {
        if (!SaveManager.ToolEnabled) return;
        if (SelectedSaveSlot.IsNewGame) return;   // 새 게임이면 로드를 건너뛴다

        WaitedLoad().Forget();
    }

    // Start()가 끝난 다음 프레임에 한 번만 로드한다
    private async UniTaskVoid WaitedLoad()
    {
        await UniTask.Yield();
        TryLoad();
    }

    // 슬롯의 두 세대 중 검증을 통과하는 최신 저장본을 적용한다.
    private bool TryLoad()
    {
        if (!saveSlot.TryReadLatest(SelectedSaveSlot.SlotId, out SaveFile file)) return false;

        ApplyLoaded(file.saveData);
        return true;
    }

    // 검증된 데이터를 적용하고, DayStart면 생산·완벽방어 보상을 정확히 1회 더 실행한다.
    private void ApplyLoaded(SaveData data)
    {
        saveRestore.RestoreSaveData(data);
        saveTimeData.SetPlayTime(data.playTime);
        saveTimeData.SetDayList(data.savedDayList);
        dayNightButton.RefreshDayText();

        if (data.savePhase != SavePhase.DayStart) return;

        saveRestore.ApplyProduction();
        if (data.perfectDefensePending)
        {
            saveRestore.ApplyPerfectDefenseReward();
        }
    }
}
