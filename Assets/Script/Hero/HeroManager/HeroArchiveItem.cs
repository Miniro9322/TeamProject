using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeroArchiveItem : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI nameText;
    private Action<HeroData> onClick;
    private HeroData data;
    public void Setup(HeroData data, Action<HeroData> onClick)
    {
        this.data = data;
        this.onClick = onClick;
        icon.sprite = data.Icon;
        nameText.text = DataTableManager.StringTable.Get(data.HeroNameKey);
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => this.onClick(this.data));
    }
}
