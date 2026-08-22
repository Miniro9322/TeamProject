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

    public event Action SlotConfirmed;
    public event Action SaveChanged;

    private readonly SlotPreviewReader previewReader = new SlotPreviewReader();
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

    // ESC: 확인 팝업이 열려있으면 팝업만 취소하고, 아니면 패널 자체를 닫는다.
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

    // 슬롯을 고르지 않고 패널을 닫는다.
    private void OnClose()
    {
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
        StringTable table = DataTableManager.StringTable;
        string message = BuildConfirmMessage(slotId, info, table);
        string buttonLabel = GetConfirmLabel(table);

        confirmPopup.ShowPopup(message, buttonLabel, () => ConfirmSlot(slotId));
    }

    // 선택 모드와 저장 여부에 맞는 확인 문구를 만든다.
    private string BuildConfirmMessage(int slotId, SlotPreviewInfo info, StringTable table)
    {
        if (mode == SlotSelectMode.Load)
        {
            return string.Format(table.Get("Ui_LoadConfirm"), slotId);
        }

        if (info.HasSave)
        {
            return string.Format(table.Get("Ui_OverwriteWarning"), slotId);
        }

        return string.Format(table.Get("Ui_NewGameConfirm"), slotId);
    }

    // 현재 선택 모드에 맞는 확인 버튼 문구를 반환한다.
    private string GetConfirmLabel(StringTable table)
    {
        if (mode == SlotSelectMode.Load)
        {
            return table.Get("Ui_Load");
        }

        return table.Get("Ui_Confirm");
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
        SlotConfirmed?.Invoke();
    }
}
