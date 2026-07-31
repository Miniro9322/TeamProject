using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BaseUpgradeButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;

    public BaseUpgradeData Data { get; private set; }
    public event Action<BaseUpgradeData> Clicked;

    public void Set(BaseUpgradeData data, bool unlocked)
    {
        Data = data;
        icon.sprite = unlocked ? data.unlockedIcon : data.lockedIcon;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => Clicked?.Invoke(data));
    }
}
