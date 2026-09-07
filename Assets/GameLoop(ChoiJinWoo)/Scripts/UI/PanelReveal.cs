using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class PanelReveal : MonoBehaviour
{
    [SerializeField] private float scaleSpeed = 5f;

    private CancellationTokenSource cts;

    private void OnDisable()
    {
        ResetCts();
    }

    public void Show()
    {
        ResetCts();

        if (!gameObject.activeSelf)
        {
            transform.localScale = Vector3.zero;
            gameObject.SetActive(true);
        }

        OpenCor(cts.Token).Forget();
    }

    public void Hide()
    {
        ResetCts();
        CloseCor(cts.Token).Forget();
    }

    private void ResetCts()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = new CancellationTokenSource();
    }

    private async UniTaskVoid OpenCor(CancellationToken token)
    {
        float t = Progress01(transform.localScale);

        try
        {
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * scaleSpeed;
                transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }

        transform.localScale = Vector3.one;
    }

    private async UniTaskVoid CloseCor(CancellationToken token)
    {
        float t = 1f - Progress01(transform.localScale);

        try
        {
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * scaleSpeed;
                transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }

        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        cts?.Cancel();
        cts?.Dispose();
    }

    private static float Progress01(Vector3 scale) => Mathf.Clamp01(scale.x);
}
