using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

// 슬롯·저장 단계와 저장·로드 호출 순서를 조정한다.
// GameManager를 고치지 않기 위해, 이미 있는 ChangeToDay/ChangeToNight 이벤트를 구독만 해서 저장한다
// (대가: 저장이 실패해도 낮·밤 전환 자체는 막지 않는다 — 직전 정상 저장본은 그대로 안전하게 남는다).
public class SaveManager : ITickable, IStartable
{ 


    private readonly SaveSlot saveSlot;
    private readonly SaveCapture saveCapture;
    private readonly SaveRestore saveRestore;
    private readonly GameManager gameManager;
    private readonly DayNightButton dayNightButton;

    private float playTime;

    public SaveManager(
        SaveSlot saveSlot,
        SaveCapture saveCapture,
        SaveRestore saveRestore,
        GameManager gameManager,
        DayNightButton dayNightButton)
    {
        this.saveSlot = saveSlot;
        this.saveCapture = saveCapture;
        this.saveRestore = saveRestore;
        this.gameManager = gameManager;
        this.dayNightButton = dayNightButton;

        gameManager.ChangeToDay += OnDayTransitioned;
        gameManager.ChangeToNight += OnNightTransitioned;
    }

    // 모든 Start()가 끝난 다음 프레임에 한 번만 로드한다(GameManager.Start의 초기값을 로드값으로 덮어쓰기 위함).
    public void Start()
    {
        if (!ToolEnabled)
        {
            return;
        }
        if (SelectedSaveSlot.IsNewGame)
        {
             return;   // 새 게임이면 로드를 건너뛴다
         }

        LoadDeferred().Forget();
    }

    private async UniTaskVoid LoadDeferred()
    {
        await UniTask.Yield();
        TryLoad();
    }

    // 누적 플레이 시간을 매 프레임 더한다.
    public void Tick()
    {
        playTime += Time.deltaTime;
    }

    // 새 일차로 전환된 직후(생산 전) 상태를 저장한다
    private void OnDayTransitioned()
    {
        TrySave(SavePhase.DayStart);
    }

    // 밤으로 전환된 직후(낮 준비가 끝난 최종) 상태를 저장한다
    private void OnNightTransitioned()
    {
        TrySave(SavePhase.NightReady);
    }

    private bool TrySave(SavePhase phase)
    {
        if (!ToolEnabled)
        {
            return false;
        }

        SaveData data = saveCapture.CaptureSaveData(phase, playTime);
        SaveFile file = new SaveFile();
        file.fileTag = SaveCheck.FileTag;
        file.saveVersion = SaveCheck.SaveVersion;
        file.saveData = data;
        return saveSlot.TryWrite(SelectedSaveSlot.SlotId, file);
    }

    // 슬롯의 두 세대 중 검증을 통과하는 최신 저장본을 적용한다.
    private bool TryLoad()
    {
        if (!saveSlot.TryReadLatest(SelectedSaveSlot.SlotId, out SaveFile file)) return false;

        ApplyLoaded(file.saveData);
        return true;
    }

    // 검증된 데이터를 적용하고, DayStart면 생산·완벽방어 보상을 정확히 1회 더 실행한다.
    // NightReady는 데이터만 복원한다 — 밤을 자동 재개시키지 않으므로 플레이어가 밤 버튼을 다시 눌러야 한다.
    private void ApplyLoaded(SaveData data)
    {
        saveRestore.RestoreSaveData(data);
        playTime = data.playTime;
        dayNightButton.RefreshDayText();

        if (data.savePhase != SavePhase.DayStart) return;

        saveRestore.ApplyProduction();
        if (data.perfectDefensePending)
        {
            saveRestore.ApplyPerfectDefenseReward();
        }
    }

    #region Save Tools

    public const string ToolKey = "SaveLoad.ToolEnabled";

    // 개발자 Tools 메뉴에서 지정한 세이브 활성 상태를 반환한다.
    public static bool ToolEnabled
    {
        get
        {
#if UNITY_EDITOR
            return PlayerPrefs.GetInt(ToolKey, 0) == 1;
#else
            return true;
#endif
        }
    }

    #endregion
}
