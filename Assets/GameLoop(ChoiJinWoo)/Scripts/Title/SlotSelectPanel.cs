using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 슬롯 패널에서 사용할 새 게임과 불러오기 모드를 구분한다.
public enum SlotSelectMode
{
    NewGame,
    Load
}

// 모드에 맞는 슬롯 목록과 확인·삭제 흐름을 관리한다.
public class SlotSelectPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Transform rowContainer;
    [SerializeField] private SlotRowView rowPrefab;
    [SerializeField] private Button closeButton;
    [SerializeField] private ConfirmPopup confirmPopup;
    [SerializeField] private ScrollRect scrollRect;

    // 어떤 모드로 확인됐는지(새 게임/불러오기)를 같이 넘긴다 - TitleUI가 새 게임일 때만
    // 씬 전환 전에 튜토리얼 선택 화면을 끼워 넣어야 해서 구분이 필요하다.
    public event Action<SlotSelectMode> SlotConfirmed;
    public event Action SaveChanged;

    private SlotPreviewReader previewReader;
    private readonly SlotDelete slotDelete = new SlotDelete();
    private readonly List<SlotRowView> spawnedRows = new List<SlotRowView>();

    private SlotSelectMode mode;
    private Keyboard keyboard;

    // 닫기 버튼에 실행 메서드를 연결한다.
    private void Awake()
    {
        closeButton.onClick.AddListener(OnClose);
        keyboard = Keyboard.current;
    }

    // TitleUI가 만든 리더를 그대로 받아 쓴다 (자기 것을 새로 안 만듦).
    public void SetPreviewReader(SlotPreviewReader reader)
    {
        previewReader = reader;
    }

    // ESC: 가장 위에 떠 있는 창 한 겹만 닫는다 (확인 팝업 > 불러오기 방식 팝업 > 일차 선택 패널 > 슬롯 선택 패널 순).
    private void Update()
    {
        if (keyboard == null) return;
        if (!keyboard.escapeKey.wasPressedThisFrame) return;

        if (confirmPopup.gameObject.activeSelf)
        {
            confirmPopup.Cancel();
            return;
        }

        OnClose();
    }

    // 새 게임 모드로 전체 슬롯 목록을 연다.
    public void OpenForNewGame()
    {
        mode = SlotSelectMode.NewGame;
        titleText.text = DataTableManager.StringTable.Get("Ui_NewGamePanelTitle");
        BuildRows(true);
        gameObject.SetActive(true);
        ResetScroll();
    }

    // 불러오기 모드로 저장된 슬롯 목록만 연다.
    public void OpenForLoad()
    {
        mode = SlotSelectMode.Load;
        titleText.text = DataTableManager.StringTable.Get("Ui_LoadPanelTitle");
        BuildRows(false);
        gameObject.SetActive(true);
        ResetScroll();
    }

    // 슬롯을 고르지 않고 패널과 열려있는 팝업을 모두 닫는다.
    private void OnClose()
    {
        confirmPopup.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    // 현재 모드에 필요한 슬롯 행을 다시 만든다.
    private void BuildRows(bool includeEmpty)
    {
        ClearRows();

        for (int slotId = 1; slotId <= SaveSlotConfig.SlotCount; slotId++)
        {
            SlotPreviewInfo info = previewReader.ReadSaveSlot(slotId);
            if (!includeEmpty && !info.HasSave)
            {
                continue;
            }

            SlotRowView row = Instantiate(rowPrefab, rowContainer);
            bool canDelete = mode == SlotSelectMode.Load;
            row.BindSlot(slotId, info, canDelete, OnRowClicked, OnDeleteClicked);
            spawnedRows.Add(row);
        }
    }

    // 이전에 생성한 모든 슬롯 행을 제거한다.
    private void ClearRows()
    {
        for (int index = 0; index < spawnedRows.Count; index++)
        {
            Destroy(spawnedRows[index].gameObject);
        }

        spawnedRows.Clear();
    }

    // 목록의 스크롤 위치를 가장 위로 되돌린다.
    private void ResetScroll()
    {
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1f;
    }

    // 슬롯 선택에 맞는 확인 팝업을 연다.
    private void OnRowClicked(int slotId, SlotPreviewInfo info)
    {
        if (mode == SlotSelectMode.Load)
        {
            ConfirmContinueLoad(slotId);
            return;
        }

        StringTable table = DataTableManager.StringTable;
        string message = BuildConfirmMessage(slotId, info, table);
        confirmPopup.ShowPopup(message, table.Get("Ui_Confirm"), () => ConfirmSlot(slotId));
    }

    // 선택한 슬롯의 일차 목록 패널을 연다.
    // private void OpenDaySelect(int slotId)
    // {
    //     daySelectPanel.Open(slotId);
    // }

    // 이어하기 선택 시 최종 확인 팝업을 연다.
    private void ConfirmContinueLoad(int slotId)
    {
        StringTable table = DataTableManager.StringTable;
        string message = string.Format(table.Get("Ui_LoadConfirm"), slotId);
        confirmPopup.ShowPopup(message, table.Get("Ui_Load"), () => ConfirmSlot(slotId));
    }

    // 새 게임 모드의 저장 여부에 맞는 확인 문구를 만든다.
    private string BuildConfirmMessage(int slotId, SlotPreviewInfo info, StringTable table)
    {
        if (info.HasSave)
        {
            return string.Format(table.Get("Ui_OverwriteWarning"), slotId);
        }

        return string.Format(table.Get("Ui_NewGameConfirm"), slotId);
    }

    // 슬롯 삭제 경고 팝업을 연다.
    private void OnDeleteClicked(int slotId)
    {
        StringTable table = DataTableManager.StringTable;
        string message = string.Format(table.Get("Ui_DeleteConfirm"), slotId);
        string buttonLabel = table.Get("Ui_Delete");

        confirmPopup.ShowPopup(message, buttonLabel, () => DeleteSlot(slotId));
    }

    // 선택한 슬롯을 삭제하고 불러오기 목록을 갱신한다.
    private void DeleteSlot(int slotId)
    {
        if (!slotDelete.TryDelete(slotId))
        {
            Debug.LogError($"[SaveLoad] Slot {slotId} 삭제에 실패했습니다.");
            return;
        }

        previewReader.ClearCache();
        SaveChanged?.Invoke();
        BuildRows(false);

        if (spawnedRows.Count == 0)
        {
            gameObject.SetActive(false);
            return;
        }

        ResetScroll();
    }

    // 확인된 슬롯을 새 게임 또는 불러오기 대상으로 저장한다.
    private void ConfirmSlot(int slotId)
    {
        if (mode == SlotSelectMode.Load)
        {
            SelectedSaveSlot.SetLoad(slotId);
        }
        else
        {
            SelectedSaveSlot.SetNewGame(slotId);
        }

        gameObject.SetActive(false);
        SlotConfirmed?.Invoke(mode);
    }
}
