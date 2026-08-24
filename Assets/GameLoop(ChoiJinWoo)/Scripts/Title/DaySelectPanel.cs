using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 슬롯 하나의 저장된 일차 목록을 보여주고 선택·확인 흐름을 관리한다.
public class DaySelectPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text emptyText;
    [SerializeField] private Transform rowContainer;
    [SerializeField] private DayRowView rowPrefab;
    [SerializeField] private Button closeButton;
    [SerializeField] private ConfirmPopup confirmPopup;
    [SerializeField] private ScrollRect scrollRect;

    public event Action SlotConfirmed;

    private readonly DaySelectReader dayReader = new DaySelectReader();
    private readonly List<DayRowView> spawnedRows = new List<DayRowView>();

    private int slotId;

    // 닫기 버튼에 실행 메서드를 연결한다.
    private void Awake()
    {
        closeButton.onClick.AddListener(OnClose);
    }

    // 지정한 슬롯의 일차 목록을 연다.
    public void Open(int slotId)
    {
        this.slotId = slotId;
        titleText.text = DataTableManager.StringTable.Get("Ui_DaySelectPanelTitle");
        emptyText.text = DataTableManager.StringTable.Get("Ui_DaySelectEmpty");
        BuildRows();
        gameObject.SetActive(true);
        ResetScroll();
    }

    // 일차를 고르지 않고 패널과 열려있는 확인 팝업을 모두 닫는다.
    private void OnClose()
    {
        confirmPopup.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    // 슬롯의 일차 목록으로 행을 다시 만든다.
    private void BuildRows()
    {
        ClearRows();

        DayPreviewInfo[] dayList = dayReader.ReadDayList(slotId);
        emptyText.gameObject.SetActive(dayList.Length == 0);

        for (int i = 0; i < dayList.Length; i++)
        {
            DayRowView row = Instantiate(rowPrefab, rowContainer);
            row.BindDay(dayList[i], OnRowClicked);
            spawnedRows.Add(row);
        }
    }

    // 이전에 생성한 모든 일차 행을 제거한다.
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

    // 선택한 일차에 대한 경고 문구로 확인 팝업을 연다 (목록엔 과거 일차만 있어 항상 경고 대상).
    private void OnRowClicked(int dayCount)
    {
        StringTable table = DataTableManager.StringTable;
        string message = table.Get("Ui_DaySelectWarning");

        confirmPopup.ShowPopup(message, table.Get("Ui_Load"), () => ConfirmDay(dayCount));
    }

    // 확인된 일차를 로드 대상으로 저장한다.
    private void ConfirmDay(int dayCount)
    {
        SelectedSaveSlot.SetLoadDay(slotId, dayCount);
        gameObject.SetActive(false);
        SlotConfirmed?.Invoke();
    }
}
