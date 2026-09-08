using System;
using UnityEngine;

public class TutorialActivityWatcher : MonoBehaviour
{
    public static event Action Changed;

    private void OnEnable() => Changed?.Invoke();
    private void OnDisable() => Changed?.Invoke();
}
