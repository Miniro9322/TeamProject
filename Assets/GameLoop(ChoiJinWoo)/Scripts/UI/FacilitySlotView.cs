using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 구조물 그리드의 칸 하나. 빈 칸/지어진 칸을 오브젝트로 나누지 않고 아이콘 스프라이트 + 텍스트만
// 바꿔서 표현한다. 레벨/배치 인력은 생산 시설에만 있는 개념이라 House는 빈 문자열로 넘어온다.
public class FacilitySlotView : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Button button;

    private int index;
    public int Index => index;

    public void SetIndex(int value)
    {
        index = value;
    }

    public void ShowEmpty()
    {
        if (icon != null) icon.gameObject.SetActive(false);
        if (nameText != null) nameText.text = "+";
    }

    public void ShowBuilt(Sprite facilityIcon, string label, string level, string workers)
    {
        if (icon != null)
        {
            icon.sprite = facilityIcon;
            icon.gameObject.SetActive(true);
        }
        if (nameText != null) nameText.text = $"{level} {label}\n{workers}".Trim();
    }

    public void BindClick(Action<int> onClick)
    {
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick(index));
    }
}
