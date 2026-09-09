using System;
using UnityEngine.InputSystem;

public static class GlobalUiInputSignals
{
    private static InputAction clickAction;
    private static InputAction escapeAction;
    private static bool enabled;

    public static event Action ClickPerformed;

    // Esc는 우선순위 순으로 전달하고, ConsumeEscape()가 불리면 그 아래 단계는 건너뛴다.
    public static event Action EscapePerformed;          // 모달/중첩 패널
    public static event Action EscapePerformedLow;       // 상주 패널(RegionOverviewPanel)
    public static event Action EscapePerformedFallback;  // UiManager 메뉴 열기 / TitleUI 종료창

    private static bool escapeConsumed;

    // 열린 패널이 이번 Esc를 처리했음을 알린다(한 번의 디스패치 동안만 유효).
    public static void ConsumeEscape() => escapeConsumed = true;

    public static void Enable()
    {
        if (enabled) return;
        enabled = true;

        clickAction = new InputAction("GlobalClick", InputActionType.Button, "<Mouse>/leftButton");
        clickAction.performed += OnClickPerformed;
        clickAction.Enable();

        escapeAction = new InputAction("GlobalEscape", binding: "<Keyboard>/escape");
        escapeAction.performed += OnEscapePerformed;
        escapeAction.Enable();
    }

    public static void Disable()
    {
        if (!enabled) return;
        enabled = false;

        clickAction.performed -= OnClickPerformed;
        clickAction.Disable();
        clickAction.Dispose();
        clickAction = null;

        escapeAction.performed -= OnEscapePerformed;
        escapeAction.Disable();
        escapeAction.Dispose();
        escapeAction = null;
    }

    private static void OnClickPerformed(InputAction.CallbackContext context) => ClickPerformed?.Invoke();

    private static void OnEscapePerformed(InputAction.CallbackContext context)
    {
        escapeConsumed = false;

        EscapePerformed?.Invoke();
        if (escapeConsumed) return;

        EscapePerformedLow?.Invoke();
        if (escapeConsumed) return;

        EscapePerformedFallback?.Invoke();
    }
}
