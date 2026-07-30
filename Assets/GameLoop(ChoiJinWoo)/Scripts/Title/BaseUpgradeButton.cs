using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BaseUpgradeButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private Sprite unlockedSprite;

    public BaseUpgradeData Data { get; private set; }
    public event Action<BaseUpgradeData> Clicked;

    public void Set(BaseUpgradeData data, bool unlocked)
    {
        Data = data;
        icon.sprite = unlocked ? unlockedSprite : lockedSprite;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => Clicked?.Invoke(data));
    }
}
