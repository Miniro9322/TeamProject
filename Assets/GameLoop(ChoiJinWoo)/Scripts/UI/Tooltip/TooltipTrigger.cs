using UnityEngine;
using UnityEngine.EventSystems;

// 버튼 등 UI 오브젝트에 붙이면 마우스 호버 시 TooltipUi에 message를 띄운다.
public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField, TextArea] private string message;

    public void OnPointerEnter(PointerEventData eventData)
    {
        TooltipUi.Instance.Show(message, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipUi.Instance.Hide();
    }

    private void OnDisable()
    {
        // 호버 도중 버튼/패널이 꺼지면(패널 토글 등) PointerExit이 안 불릴 수 있어 여기서도 닫아준다.
        if (TooltipUi.Instance != null) TooltipUi.Instance.Hide();
    }

    public void SetMessaege(string message)
    {
        this.message = message;
    }
}
