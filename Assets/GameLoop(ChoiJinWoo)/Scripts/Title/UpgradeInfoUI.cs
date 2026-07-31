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

    private void Awake()
    {
        confirmButton.onClick.AddListener(() => ConfirmClicked?.Invoke(current));
    }

    public void Show(BaseUpgradeData data, bool canUnlockNow)
    {
        current = data;
        nameText.text = data.displayName;
        descText.text = data.description;
        costText.text = data.cost.ToString();
        icon.sprite = data.lockedIcon;
        confirmButton.gameObject.SetActive(canUnlockNow); // 여기서만 잠금 여부가 반영됨
        gameObject.SetActive(true);
    }
}
