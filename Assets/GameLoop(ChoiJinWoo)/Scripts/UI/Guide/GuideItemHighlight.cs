using UnityEngine;
using UnityEngine.UI;

public class GuideItemHighlight : MonoBehaviour
{
    [SerializeField] private Image itemImage;
    [SerializeField] private Color selectedTint = new Color(0.502f, 0.361f, 0.204f);
    [SerializeField] private Color normalTint = Color.white;
    [SerializeField] private GuideClickBurst clickBurst;

    // 이 항목을 선택 상태로 표시하거나 해제하고, 선택될 때만(+요청 시) 클릭 스파크를 띄운다
    public void SetSelected(bool selected, bool playBurst = true)
    {
        if (selected)
        {
            itemImage.color = selectedTint;
            if (playBurst) clickBurst.SpawnAt(itemImage.rectTransform);
            return;
        }
        itemImage.color = normalTint;
    }
}
