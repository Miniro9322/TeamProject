using System;
using UnityEngine.InputSystem;

public static class GlobalUiInputSignals
{
    private static InputAction clickAction;
    private static InputAction escapeAction;
    private static bool enabled;

    public static event Action ClickPerformed;

    // Esc는 우선순위 3단계로 나눠 전달한다. 위 단계에서 누군가 ConsumeEscape()로 "이번 Esc는 내가
    // 처리했다"고 표시하면 아래 단계는 호출되지 않는다. 예전엔 모든 구독자가 같은 이벤트를 동시에
    // 받아서, 낮은 우선순위 쪽(UiManager의 메뉴 열기 등)이 "이번 Esc를 다른 패널이 이미 처리했는지"를
    // 알 수 없어 각자 LateUpdate로 지난 프레임 상태를 폴링해야 했다. 이제는 디스패치가 그 자리에서
    // 순서대로 물어보므로 폴링이 필요 없다.
    //   1) EscapePerformed        - 열려 있는 모달/중첩 패널들(메뉴, 가이드, 지역 상세 등)
    //   2) EscapePerformedLow     - 상주 패널(RegionOverviewPanel). 위의 모달이 떠 있으면 그쪽이 먼저 먹는다.
    //   3) EscapePerformedFallback - 아무도 안 먹었을 때만(UiManager: 메뉴 열기 / TitleUI: 종료 확인창)
    public static event Action EscapePerformed;
    public static event Action EscapePerformedLow;
    public static event Action EscapePerformedFallback;

    private static bool escapeConsumed;

    // 열려 있는 패널이 이번 Esc 입력을 처리(닫기/취소)했음을 알린다 - 이 아래 우선순위 단계는 건너뛴다.
    // 한 번의 Esc 디스패치 안에서만 유효하며 다음 Esc 때 다시 false로 초기화된다.
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
