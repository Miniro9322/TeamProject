using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 구조물 그리드의 칸 하나. 비어있으면 "건설" 상태, 지어져 있으면 아이콘+레벨을 보여준다.
public class FacilitySlotView : MonoBehaviour
{
    [SerializeField] private GameObject emptyState;
    [SerializeField] private GameObject builtState;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Button button;

    private int index;
    public int Index => index;

    public void SetIndex(int value)
    {
        index = value;
    }

    public void ShowEmpty()
    {
        emptyState.SetActive(true);
        builtState.SetActive(false);
    }

    public void ShowBuilt(Sprite facilityIcon, string levelLabel)
    {
        emptyState.SetActive(false);
        builtState.SetActive(true);
        if (icon != null) icon.sprite = facilityIcon;
        if (levelText != null) levelText.text = levelLabel;
    }

    public void BindClick(Action<int> onClick)
    {
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick(index));
    }
}
