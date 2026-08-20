using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 슬롯 패널의 진입 모드. 새 게임은 전체 슬롯, 불러오기는 채워진 슬롯만 보여준다.
public enum SlotSelectMode
{
    NewGame,
    Load
}

// SaveSlotConfig.SlotCount만큼 슬롯 행을 만들고, 모드에 맞는 목록·팝업 문구를 골라 보여준다.
public class SlotSelectPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Transform rowContainer;
    [SerializeField] private SlotRowView rowPrefab;
    [SerializeField] private Button closeButton;
    [SerializeField] private ConfirmPopup confirmPopup;

    public event Action SlotConfirmed;

    private readonly SlotPreviewReader previewReader = new SlotPreviewReader();
    private readonly List<SlotRowView> spawnedRows = new List<SlotRowView>();
    private SlotSelectMode mode;

    private void Awake()
    {
        closeButton.onClick.AddListener(OnClose);
    }

    // "start" 버튼에서 연다 — 빈 슬롯 포함 전체를 보여준다.
    public void OpenForNewGame()
    {
        mode = SlotSelectMode.NewGame;
        titleText.text = DataTableManager.StringTable.Get("Ui_NewGamePanelTitle");
        BuildRows(includeEmpty: true);
        gameObject.SetActive(true);
    }

    // "Load" 버튼에서 연다 — 채워진 슬롯만 보여준다.
    public void OpenForLoad()
    {
        mode = SlotSelectMode.Load;
        titleText.text = DataTableManager.StringTable.Get("Ui_LoadPanelTitle");
        BuildRows(includeEmpty: false);
        gameObject.SetActive(true);
    }

    // 슬롯을 고르지 않고 패널을 닫는다.
    private void OnClose()
    {
        gameObject.SetActive(false);
    }

    // 기존 행을 지우고 SaveSlotConfig.SlotCount만큼 다시 만든다(빈도가 낮아 풀링 없이 단순하게 처리).
    private void BuildRows(bool includeEmpty)
    {
        ClearRows();

        for (int slotId = 1; slotId <= SaveSlotConfig.SlotCount; slotId++)
        {
            SlotPreviewInfo info = previewReader.ReadSaveSlot(slotId);
            if (!includeEmpty && !info.HasSave) continue;

            SlotRowView row = Instantiate(rowPrefab, rowContainer);
            row.BindSlot(slotId, info, OnRowClicked);
            spawnedRows.Add(row);
        }
    }

    // 다음 BuildRows 전에 이전 행을 전부 지운다.
    private void ClearRows()
    {
        for (int i = 0; i < spawnedRows.Count; i++)
        {
            Destroy(spawnedRows[i].gameObject);
        }
        spawnedRows.Clear();
    }

    // 행 클릭 시 모드에 맞는 확인 문구를 골라 팝업을 띄운다.
    private void OnRowClicked(int slotId)
    {
        SlotPreviewInfo info = previewReader.ReadSaveSlot(slotId);
        string message = BuildConfirmMessage(slotId, info);

        confirmPopup.ShowPopup(message, () => ConfirmSlot(slotId));
    }

    // 모드·저장 여부 조합으로 팝업 문구를 고른다.
    private string BuildConfirmMessage(int slotId, SlotPreviewInfo info)
    {
        StringTable table = DataTableManager.StringTable;

        if (mode == SlotSelectMode.Load)
        {
            return string.Format(table.Get("Ui_LoadConfirm"), slotId);
        }

        if (info.HasSave)
        {
            return string.Format(table.Get("Ui_OverwriteWarning"), slotId);
        }

        return table.Get("Ui_NewGameConfirm");
    }

    // 확인 팝업 통과 후 실제로 슬롯을 확정하고 상위(TitleUI)에 알린다.
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
