using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using VContainer;

public class CenterFeedbackUi : MonoBehaviour
{
    [SerializeField] private RectTransform template;
    [SerializeField] private int maxActive = 6;
    [SerializeField] private float riseDistance = 60f;
    [SerializeField] private float holdDuration = 0.4f;
    [SerializeField] private float fadeDuration = 0.6f;

    private Vector2 basePosition;
    private readonly Stack<RectTransform> pool = new();
    private readonly Queue<RectTransform> active = new();
    private readonly Dictionary<RectTransform, CancellationTokenSource> playing = new();

    private TooltipUi tooltipUi;

    [Inject]
    private void Construct(TooltipUi tooltipUi)
    {
        this.tooltipUi = tooltipUi;
    }

    private void Awake()
    {
        basePosition = template.anchoredPosition;
        template.gameObject.SetActive(false);
    }

    public void Show(string messageKey)
    {
        if (string.IsNullOrEmpty(messageKey)) return;

        if (tooltipUi != null) tooltipUi.Hide();

        RectTransform instance = Rent();
        active.Enqueue(instance);

        instance.anchoredPosition = basePosition;

        var text = instance.GetComponent<TextMeshProUGUI>();
        text.text = DataTableManager.StringTable.Get(messageKey);

        var canvasGroup = instance.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;

        instance.gameObject.SetActive(true);

        var cts = new CancellationTokenSource();
        playing[instance] = cts;

        Play(instance, canvasGroup, cts.Token).Forget();
    }

    private RectTransform Rent()
    {
        if (pool.Count > 0) return pool.Pop();

        if (active.Count < maxActive)
        {
            RectTransform created = Instantiate(template, template.parent);

            CanvasGroup canvasGroup = created.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = created.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            return created;
        }

        RectTransform oldest = active.Dequeue();
        if (playing.TryGetValue(oldest, out var oldestCts))
        {
            oldestCts.Cancel();
            oldestCts.Dispose();
            playing.Remove(oldest);
        }

        return oldest;
    }

    private async UniTaskVoid Play(RectTransform instance, CanvasGroup canvasGroup, CancellationToken token)
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(holdDuration), ignoreTimeScale: true, cancellationToken: token);

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / fadeDuration;
                instance.anchoredPosition = basePosition + new Vector2(0f, riseDistance * t);
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }

        playing.Remove(instance);
        active.Dequeue();
        instance.gameObject.SetActive(false);
        pool.Push(instance);
    }
}
