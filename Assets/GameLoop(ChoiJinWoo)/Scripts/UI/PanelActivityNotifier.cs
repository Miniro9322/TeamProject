using System;
using UnityEngine;

public class PanelActivityNotifier : MonoBehaviour
{
    public event Action<bool> ActiveChanged;

    private void OnEnable() => ActiveChanged?.Invoke(true);
    private void OnDisable() => ActiveChanged?.Invoke(false);
}
