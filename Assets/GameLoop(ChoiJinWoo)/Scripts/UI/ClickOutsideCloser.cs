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

    // Esc로 닫는 경우 전역 신호에 소비를 표시해 하위 우선순위(메뉴 열기 등)가 같은 Esc에 반응하지 않게 한다.
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
