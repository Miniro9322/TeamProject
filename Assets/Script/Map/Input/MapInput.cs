using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// 마우스 왼쪽 버튼의 눌림/뗌을 매 프레임 감지해 알린다.
public class MapInput : MonoBehaviour
{
    public event Action Pressed;
    public event Action Released;
    public event Action RightPressed;
    public event Action RightReleased;

    [SerializeField] private bool _blocked;

    public bool Blocked
    {
        get { return _blocked; }
    }

    private void Update()
    {
        if (_blocked) return;
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (Mouse.current.leftButton.wasPressedThisFrame) Pressed?.Invoke();
        if (Mouse.current.leftButton.wasReleasedThisFrame) Released?.Invoke();
        if (Mouse.current.rightButton.wasPressedThisFrame) RightPressed?.Invoke();
        if (Mouse.current.rightButton.wasReleasedThisFrame) RightReleased?.Invoke();
    }

    public void SetBlock(bool value)
    {
        _blocked = value;
    }
}
