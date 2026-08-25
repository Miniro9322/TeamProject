using UnityEngine;
using UnityEngine.InputSystem;

// 게임 화면에서 마우스를 따라다니는 커스텀 커서의 위치와 눌림 상태를 갱신한다.
public class CustomCursorFollow : MonoBehaviour
{
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private Animator cursorAnimator;

    private static readonly int PressedParameter = Animator.StringToHash("IsPressed");

    private RectTransform cursorRect;

    // 커서 자신의 RectTransform을 캐시한다.
    private void Awake()
    {
        cursorRect = (RectTransform)transform;
    }

    // 활성화되는 동안 시스템 커서를 숨긴다.
    private void OnEnable()
    {
        Cursor.visible = false;
    }

    // 비활성화되면 시스템 커서를 되돌린다.
    private void OnDisable()
    {
        Cursor.visible = true;
    }

    // 매 프레임 마우스 좌표로 커서 위치를 옮기고 눌림 상태를 Animator에 전달한다.
    private void Update()
    {
        cursorRect.anchoredPosition = ScreenToCanvasPosition(Mouse.current.position.ReadValue());
        cursorAnimator.SetBool(PressedParameter, Mouse.current.leftButton.isPressed);
    }

    // 화면 좌표를 캔버스 기준 로컬 좌표로 계산한다.
    private Vector2 ScreenToCanvasPosition(Vector2 screenPosition)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out localPoint);
        return localPoint;
    }
}
