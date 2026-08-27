using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 패널이 뜰 때 가이드 패널(GuidePanelEnter.anim)과 같은 느낌으로 살짝 튀어오르듯 나타나게 하는 유틸리티.
// 별도 Animator/애니메이션 클립 없이 패널의 "쉬는" 스케일/위치를 기준으로 상대적으로 보간하므로,
// 어떤 패널에 써도 그 패널 고유의 배치를 건드리지 않는다. SetActive(true) 직후에 호출한다.
public static class PanelPopIn
{
    private const float Duration = 0.3f;
    private const float StartScale = 0.92f;
    private const float StartOffsetY = -24f;

    private static readonly Dictionary<RectTransform, CancellationTokenSource> playing = new();

    public static void Play(RectTransform panel)
    {
        if (playing.TryGetValue(panel, out var prev))
        {
            prev.Cancel();
            prev.Dispose();
        }

        var cts = new CancellationTokenSource();
        playing[panel] = cts;
        PlayAsync(panel, cts.Token).Forget();
    }

    private static async UniTaskVoid PlayAsync(RectTransform panel, CancellationToken token)
    {
        Vector3 restScale = panel.localScale;
        Vector2 restPos = panel.anchoredPosition;

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
    }

    // 가이드 패널과 같은 살짝 튀어오르는(오버슈트) 감속 곡선.
    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }
}
