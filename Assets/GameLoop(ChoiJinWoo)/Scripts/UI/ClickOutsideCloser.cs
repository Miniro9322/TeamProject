using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// 열린 바로 그 프레임의 클릭은 무시하고, 그 다음부터 자기(또는 자기 자식)가 아닌 곳을 클릭하면 true를 돌려준다.
// 사각형 범위(RectTransformUtility.RectangleContainsScreenPoint)로 판정하면 Canvas 렌더 모드나
// 레이아웃 구조에 따라 자기 자신의 버튼 클릭까지 "바깥"으로 오판할 수 있어서, 실제 UI 레이캐스트
// 결과가 자기 자신 하위 트리에 속하는지로 판정한다.
public class ClickOutsideCloser
{
    private readonly Transform root;
    private int openedFrame;
    private static readonly List<RaycastResult> raycastResults = new();

    public ClickOutsideCloser(Transform root)
    {
        this.root = root;
    }

    public void MarkOpened()
    {
        openedFrame = Time.frameCount;
    }

    public bool ClickedOutside()
    {
        if (Time.frameCount == openedFrame) return false;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return false;
        if (EventSystem.current == null) return true;

        var pointerData = new PointerEventData(EventSystem.current) { position = Mouse.current.position.ReadValue() };
        raycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        foreach (var result in raycastResults)
        {
            if (result.gameObject.transform.IsChildOf(root)) return false; // 자기 자신(자식 포함) 클릭이면 바깥이 아니다
        }

        return true;
    }
}
