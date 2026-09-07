using System;
using UnityEngine.InputSystem;

public static class GlobalUiInputSignals
{
    private static InputAction clickAction;
    private static InputAction escapeAction;
    private static bool enabled;

    public static event Action ClickPerformed;
    public static event Action EscapePerformed;

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
    private static void OnEscapePerformed(InputAction.CallbackContext context) => EscapePerformed?.Invoke();
}
