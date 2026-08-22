using UnityEngine;

// 슬롯·저장 단계와 저장 호출 순서를 조정한다. 로드는 LoadManager가 전담한다.
// GameManager를 고치지 않기 위해, 이미 있는 ChangeToDay/ChangeToNight 이벤트를 구독만 해서 저장한다
// (대가: 저장이 실패해도 낮·밤 전환 자체는 막지 않는다 — 직전 정상 저장본은 그대로 안전하게 남는다).
public class SaveManager
{


    private readonly SaveSlot saveSlot;
    private readonly SaveCapture saveCapture;
    private readonly GameManager gameManager;
    private readonly SaveTimeData saveTimeData;

    public SaveManager(
        SaveSlot saveSlot,
        SaveCapture saveCapture,
        GameManager gameManager,
        SaveTimeData saveTimeData)
    {
        this.saveSlot = saveSlot;
        this.saveCapture = saveCapture;
        this.gameManager = gameManager;
        this.saveTimeData = saveTimeData;

        gameManager.ChangeToDay += OnDayTransitioned;
        gameManager.ChangeToNight += OnNightTransitioned;
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

        int[] savedDayList = saveTimeData.DayList;
        if (phase == SavePhase.DayStart)
        {
            savedDayList = DayListCalc.AppendDay(saveTimeData.DayList, gameManager.DayCount);
        }

        SaveData data = saveCapture.CaptureSaveData(phase, saveTimeData.PlayTime, savedDayList);
        SaveFile file = new SaveFile();
        file.fileTag = SaveCheck.FileTag;
        file.saveVersion = SaveCheck.SaveVersion;
        file.saveData = data;

        bool written = saveSlot.TryWrite(SelectedSaveSlot.SlotId, file);
        if (written)
        {
            saveTimeData.SetDayList(savedDayList);
        }
        return written;
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
