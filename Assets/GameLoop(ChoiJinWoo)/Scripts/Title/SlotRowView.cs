using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 슬롯 한 줄의 정보 표시와 선택·삭제 입력을 담당한다.
public class SlotRowView : MonoBehaviour
{
    private const int SecondsPerHour = 3600;
    private const int SecondsPerMinute = 60;

    [SerializeField] private Button rowButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private TMP_Text deleteText;
    [SerializeField] private TMP_Text slotText;
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text playTimeText;
    [SerializeField] private TMP_Text saveTimeText;

    private int slotId;
    private SlotPreviewInfo slotInfo;
    private Action<int> onRowClicked;
    private Action<int> onDeleteClicked;

    // 슬롯 버튼과 삭제 버튼에 실행 메서드를 연결한다.
    private void Awake()
    {
        rowButton.onClick.AddListener(OnClicked);
        deleteButton.onClick.AddListener(OnDelete);
    }

    // 언어 변경 시 현재 슬롯 문구를 다시 표시한다.
    private void OnEnable()
    {
        LocalizeTextManager.OnLanguageChanged += RefreshText;
    }

    // 비활성화된 행의 언어 변경 구독을 해제한다.
    private void OnDisable()
    {
        LocalizeTextManager.OnLanguageChanged -= RefreshText;
    }

    // 슬롯 정보와 선택·삭제 동작을 받아 행을 준비한다.
    public void BindSlot(
        int slotId,
        SlotPreviewInfo info,
        bool canDelete,
        Action<int> onRowClicked,
        Action<int> onDeleteClicked)
    {
        this.slotId = slotId;
        slotInfo = info;
        this.onRowClicked = onRowClicked;
        this.onDeleteClicked = onDeleteClicked;

        deleteButton.gameObject.SetActive(canDelete);
        RefreshText();
    }

    // 현재 언어로 슬롯의 모든 표시 문구를 갱신한다.
    private void RefreshText()
    {
        StringTable table = DataTableManager.StringTable;
        slotText.text = string.Format(table.Get("Ui_SlotLabel"), slotId);
        deleteText.text = table.Get("Ui_Delete");

        if (!slotInfo.HasSave)
        {
            SetEmptyText(table);
            return;
        }

        SetSaveText(table);
    }

    // 빈 슬롯에 새 게임 시작 문구만 표시한다.
    private void SetEmptyText(StringTable table)
    {
        dayText.text = table.Get("Ui_SlotEmptyAction");
        playTimeText.text = string.Empty;
        saveTimeText.text = string.Empty;
    }

    // 저장된 슬롯의 진행도·플레이 시간·저장 시각을 표시한다.
    private void SetSaveText(StringTable table)
    {
        dayText.text = string.Format(table.Get("Ui_SlotDay"), slotInfo.DayCount);
        playTimeText.text = string.Format(
            table.Get("Ui_PlayTimeFormat"),
            GetHours(slotInfo.PlayTime),
            GetMinutes(slotInfo.PlayTime));
        saveTimeText.text = string.Format(table.Get("Ui_SlotSaveTime"), GetSaveTime(slotInfo.SaveTime));
    }

    // 누적 플레이 시간에서 시간 단위를 구한다.
    private int GetHours(float playTime)
    {
        return (int)(playTime / SecondsPerHour);
    }

    // 누적 플레이 시간에서 분 단위를 구한다.
    private int GetMinutes(float playTime)
    {
        int remainder = (int)playTime % SecondsPerHour;
        return remainder / SecondsPerMinute;
    }

    // 저장 시각을 슬롯 화면용 짧은 형식으로 바꾼다.
    private string GetSaveTime(string saveTime)
    {
        bool parsed = DateTime.TryParse(
            saveTime,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out DateTime savedAt);

        if (!parsed)
        {
            return "-";
        }

        return savedAt.ToString("MM-dd HH:mm", CultureInfo.InvariantCulture);
    }

    // 슬롯 선택을 슬롯 번호와 함께 전달한다.
    private void OnClicked()
    {
        onRowClicked.Invoke(slotId);
    }

    // 슬롯 삭제 요청을 슬롯 번호와 함께 전달한다.
    private void OnDelete()
    {
        onDeleteClicked.Invoke(slotId);
    }
}
