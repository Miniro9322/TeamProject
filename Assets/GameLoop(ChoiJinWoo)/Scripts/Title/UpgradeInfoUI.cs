using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeInfoUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Image icon;


    public event Action<BaseUpgradeData> ConfirmClicked;
    private BaseUpgradeData current;
    public BaseUpgradeData Current => current;

    private void Awake()
    {
        confirmButton.onClick.AddListener(() => ConfirmClicked?.Invoke(current));
    }

    private void OnEnable()
    {
        if (current != null) Bind(current);
    }

    public void Show(BaseUpgradeData data, bool canUnlockNow)
    {
        current = data;
        Bind(data);
        confirmButton.gameObject.SetActive(canUnlockNow); // 여기서만 잠금 여부가 반영됨
        gameObject.SetActive(true);
    }

    // name/desc만 갱신하던 OnEnable과, 전체를 갱신하던 Show의 표시 로직을 하나로 합친다.
    private void Bind(BaseUpgradeData data)
    {
        nameText.text = DataTableManager.StringTable.Get(data.displayName);
        descText.text = DataTableManager.StringTable.Get(data.description);
        costText.text = data.cost.ToString();
        icon.sprite = data.icon;
        icon.color = data.iconColor;
    }

    public void OnReset(bool canUnlock)
    {
        confirmButton.gameObject.SetActive(canUnlock);
    }
}
