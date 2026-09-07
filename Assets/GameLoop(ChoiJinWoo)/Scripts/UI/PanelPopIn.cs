using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public static class PanelPopIn
{
    private const float Duration = 0.3f;
    private const float StartScale = 0.92f;
    private const float StartOffsetY = -24f;

    private static readonly Dictionary<RectTransform, CancellationTokenSource> playing = new();
    private static readonly Dictionary<RectTransform, (Vector3 scale, Vector2 pos)> rest = new();

    public static void Play(RectTransform panel)
    {
        if (playing.TryGetValue(panel, out var prev))
        {
            prev.Cancel();
            prev.Dispose();
        }
        else
        {
            rest[panel] = (panel.localScale, panel.anchoredPosition);
        }

        var cts = new CancellationTokenSource();
        playing[panel] = cts;
        var (restScale, restPos) = rest[panel];
        PlayAsync(panel, restScale, restPos, cts.Token).Forget();
    }

    private static async UniTaskVoid PlayAsync(RectTransform panel, Vector3 restScale, Vector2 restPos, CancellationToken token)
    {
        try
        {
            float elapsed = 0f;
            while (elapsed < Duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float eased = EaseOutBack(Mathf.Clamp01(elapsed / Duration));
                panel.localScale = Vector3.LerpUnclamped(restScale * StartScale, restScale, eased);
                panel.anchoredPosition = restPos + Vector2.LerpUnclamped(new Vector2(0f, StartOffsetY), Vector2.zero, eased);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        catch (System.OperationCanceledException)
        {
            return;
        }

        panel.localScale = restScale;
        panel.anchoredPosition = restPos;
        playing.Remove(panel);
        rest.Remove(panel);
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }
}
