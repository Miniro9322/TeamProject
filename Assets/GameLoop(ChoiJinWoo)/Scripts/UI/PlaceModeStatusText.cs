using TMPro;
using UnityEngine;

// 재배치/제거 모드 상태를 텍스트로 보여준다. MapView의 상태 변경 이벤트로만 갱신되고 Update 폴링은 하지 않는다.
public class PlaceModeStatusText : MonoBehaviour
{
    [SerializeField] private MapView view;
    [SerializeField] private TextMeshProUGUI text;

    private void OnEnable()
    {
        view.OnStateChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        view.OnStateChanged -= Refresh;
    }

    private void Refresh()
    {
        if (view.IsRemoving)
        {
            text.text = "회수할 영웅을 선택 해주세요.\nesc키로 취소";
        }
        else if (view.IsReplacing)
        {
            text.text = view.IsHolding ? "배치할 칸을 선택해주세요\nesc키로 취소." : "재배치할 영웅을 선택 해주세요.\nesc키로 취소";
        }
        else
        {
            text.text = string.Empty;
        }
    }
}
