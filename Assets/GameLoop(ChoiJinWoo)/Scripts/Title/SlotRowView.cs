 using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 슬롯 한 줄의 표시 담당.
public class SlotRowView : MonoBehaviour
{
    private const int SecondsPerHour = 3600;
    private const int SecondsPerMinute = 60;
    private const string SaveTimeDisplayFormat = "MM-dd HH:mm";

    [SerializeField] private Button rowButton;
    [SerializeField] private TMP_Text slotText;
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text playTimeText;
    [SerializeField] private TMP_Text saveTimeText;

    private int slotId;
    private Action<int> onRowClicked;

    private void Awake()
    {
        rowButton.onClick.AddListener(OnClicked);
    }

    // 슬롯 번호와 미리보기 정보를 받아 화면 문구를 채운다.
    public void BindSlot(int slotId, SlotPreviewInfo info, Action<int> onRowClicked)
    {
        this.slotId = slotId;
        this.onRowClicked = onRowClicked;

        StringTable table = DataTableManager.StringTable;
        slotText.text = string.Format(table.Get("Ui_SlotLabel"), slotId);

        SetSaveInfo(info, table);
    }

    // 저장 여부에 따라 일차·플레이시간·저장시각 칸을 채우거나 "비어있음"으로 채운다.
    private void SetSaveInfo(SlotPreviewInfo info, StringTable table)
    {
        if (!info.HasSave)
        {
            dayText.text = table.Get("Ui_SlotEmpty");
            playTimeText.text = string.Empty;
            saveTimeText.text = string.Empty;
            return;
        }

        dayText.text = string.Format(table.Get("Ui_SlotDay"), info.DayCount);
        playTimeText.text = string.Format(table.Get("Ui_PlayTimeFormat"), GetHours(info.PlayTime), GetMinutes(info.PlayTime));
        saveTimeText.text = string.Format(table.Get("Ui_SlotSaveTime"), FormatSaveTime(info.SaveTime));
    }

    // 저장된 라운드트립 시각 문자열("O" 포맷)을 화면 표시용 짧은 형식으로 바꾼다.
    private string FormatSaveTime(string rawSaveTime)
    {
        DateTime saveTime = DateTime.Parse(rawSaveTime, null, System.Globalization.DateTimeStyles.RoundtripKind);
        return saveTime.ToString(SaveTimeDisplayFormat);
    }

    // 누적 플레이 시간(초)에서 시간 단위를 뽑는다.
    private int GetHours(float playTimeSeconds)
    {
        return (int)(playTimeSeconds / SecondsPerHour);
    }

    // 누적 플레이 시간(초)에서 시간을 뺀 나머지 분 단위를 뽑는다.
    private int GetMinutes(float playTimeSeconds)
    {
        int remainder = (int)playTimeSeconds % SecondsPerHour;
        return remainder / SecondsPerMinute;
    }

    // 버튼 클릭을 슬롯 번호와 함께 SlotSelectPanel로 전달한다.
    private void OnClicked()
    {
        onRowClicked.Invoke(slotId);
    }
}
