using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CustomCursorFollow : MonoBehaviour
{
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private Animator cursorAnimator;

    private static readonly int PressedParameter = Animator.StringToHash("IsPressed");

    private RectTransform cursorRect;

#if UNITY_EDITOR
    private bool isGameView;
#endif

    private void Awake()
    {
        cursorRect = (RectTransform)transform;
    }

    private void OnEnable()
    {
#if UNITY_EDITOR
        isGameView = IsGameView();
        ApplyView();
#else
        Cursor.visible = false;
#endif
    }

    private void OnDisable()
    {
        Cursor.visible = true;
        cursorAnimator.gameObject.SetActive(true);
    }

    private void Update()
    {
#if UNITY_EDITOR
        UpdateView();

        if (!isGameView)
        {
            return;
        }
#endif

        cursorRect.anchoredPosition = ScreenToCanvasPosition(Mouse.current.position.ReadValue());
        cursorAnimator.SetBool(PressedParameter, Mouse.current.leftButton.isPressed);
    }

#if UNITY_EDITOR

    private bool IsGameView()
    {
        EditorWindow window = EditorWindow.mouseOverWindow;

        if (window == null)
        {
            return false;
        }

        return window.GetType().FullName == "UnityEditor.GameView";
    }

    private void UpdateView()
    {
        bool nextView = IsGameView();

        if (isGameView == nextView)
        {
            return;
        }

        isGameView = nextView;
        ApplyView();
    }

    private void ApplyView()
    {
        cursorAnimator.gameObject.SetActive(isGameView);
        Cursor.visible = !isGameView;
    }
#endif

    private Vector2 ScreenToCanvasPosition(Vector2 screenPosition)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out localPoint);
        return localPoint;
    }
}
