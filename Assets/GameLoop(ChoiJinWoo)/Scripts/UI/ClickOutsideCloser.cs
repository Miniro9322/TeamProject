using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ClickOutsideCloser
{
    private readonly Transform root;
    private readonly Transform[] alsoSelf;
    private int openedFrame;
    private static readonly List<RaycastResult> raycastResults = new();

    public ClickOutsideCloser(Transform root, params Transform[] alsoSelf)
    {
        this.root = root;
        this.alsoSelf = alsoSelf;
    }

    public void MarkOpened()
    {
        openedFrame = Time.frameCount;
    }

    public bool EscapePressed()
    {
        if (TutorialInputGate.BlockEscapeClose) return false;
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
    }

    // Esc로 닫히는 경우엔 전역 신호에 "이번 Esc는 이 패널이 처리했다"고 표시해, 더 낮은 우선순위
    // (UiManager의 메뉴 열기 등)가 같은 Esc에 또 반응하지 않게 한다. 이 메서드는 <가드> && ShouldClose()
    // 형태로만 호출되므로(여기 도달했다는 건 가드를 통과했다는 뜻), Esc면 호출한 패널이 곧 닫힌다 -
    // 따라서 여기서 소비를 표시해도 "닫지도 않았는데 소비만 한" 상태는 생기지 않는다. 클릭 신호 쪽에서
    // 불릴 땐 EscapePressed()가 false라 소비가 표시되지 않는다.
    public bool ShouldClose()
    {
        if (EscapePressed())
        {
            GlobalUiInputSignals.ConsumeEscape();
            return true;
        }
        return ClickedOutside();
    }

    public bool ClickedOutside()
    {
        if (TutorialInputGate.BlockEscapeClose) return false;
        if (Time.frameCount == openedFrame) return false;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return false;
        if (EventSystem.current == null) return true;

        var pointerData = new PointerEventData(EventSystem.current) { position = Mouse.current.position.ReadValue() };
        raycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        foreach (var result in raycastResults)
        {
            var hit = result.gameObject.transform;
            if (hit.IsChildOf(root)) return false;

            if (alsoSelf != null)
            {
                foreach (var extra in alsoSelf)
                {
                    if (extra != null && hit.IsChildOf(extra)) return false;
                }
            }
        }

        return true;
    }
}
