using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonSfxAutoAttacher : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        var runner = new GameObject(nameof(ButtonSfxAutoAttacher));
        DontDestroyOnLoad(runner);
        runner.AddComponent<ButtonSfxAutoAttacher>();
    }

    private Selectable[] buffer = new Selectable[16];
    private readonly HashSet<Button> processed = new();

    private void Update()
    {
        int count = Selectable.allSelectableCount;
        if (buffer.Length < count) buffer = new Selectable[count];
        int copied = Selectable.AllSelectablesNoAlloc(buffer);

        for (int i = 0; i < copied; i++)
        {
            if (buffer[i] is Button button && processed.Add(button) && !button.TryGetComponent(out ButtonSfx _))
            {
                button.gameObject.AddComponent<ButtonSfx>();
            }
        }
    }
}
