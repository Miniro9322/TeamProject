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
    [SerializeField, Range(0f, 1f)] private float lockedAlpha = 0.3f;

    public BaseUpgradeData Data { get; private set; }
    public event Action<BaseUpgradeData> Clicked;

    private void Awake()
    {
        // 리스너는 한 번만 건다. 예전엔 Set()이 호출될 때마다(RefreshAll 등) Remove/Add 하며 클로저를 새로 할당했다.
        button.onClick.AddListener(() => Clicked?.Invoke(Data));
    }

    public void Set(BaseUpgradeData data, bool unlocked, bool canUnlock)
    {
        Data = data;
        icon.sprite = data.icon;
        buttonImage.sprite = unlocked ? unlockedIcon : lockedIcon;

        // NOTE: data.iconColor는 현재 이 버튼 아이콘에는 반영되지 않는다(UpgradeInfoUI에서만 사용).
        var lockedColor = buttonImage.color;
        lockedColor.a = lockedAlpha;
        icon.color = canUnlock ? buttonImage.color : lockedColor;
    }
}
