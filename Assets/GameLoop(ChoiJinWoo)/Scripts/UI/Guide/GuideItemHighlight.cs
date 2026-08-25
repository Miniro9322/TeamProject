using UnityEngine;
using UnityEngine.UI;

public class GuideItemHighlight : MonoBehaviour
{
    [SerializeField] private Image itemImage;
    [SerializeField] private Color selectedTint = new Color(0.502f, 0.361f, 0.204f);
    [SerializeField] private Color normalTint = Color.white;

    // 이 항목을 선택 상태로 표시하거나 해제한다 (클릭 이펙트는 전역 ClickEffect가 담당)
    public void SetSelected(bool selected)
    {
        if (selected)
        {
            itemImage.color = selectedTint;
            return;
        }
        itemImage.color = normalTint;
    }
}
