using System;
using UnityEngine;
using UnityEngine.UI;

public class BaseUpgradeButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private Image buttonImage;
    [SerializeField] private Sprite lockedIcon;
    [SerializeField] private Sprite unlockedIcon;

    public BaseUpgradeData Data { get; private set; }
    public event Action<BaseUpgradeData> Clicked;

    public void Set(BaseUpgradeData data, bool unlocked)
    {
        Data = data;
        icon.sprite = data.icon;
        icon.color = data.iconColor;
        buttonImage.sprite = unlocked ? unlockedIcon : lockedIcon;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => Clicked?.Invoke(data));
    }
}
