using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class HeroArchiveButton : MonoBehaviour
{ 
    [SerializeField] private GameObject archiveUI;
    private CancellationTokenSource cts;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Start()
    {
        archiveUI.SetActive(false);
        archiveUI.transform.localScale = Vector3.zero; // 닫힘 = 스케일 0 기준 (이어서 열기 진행도 계산용)
        isOpen = false;
    }

    private void OnDestroy()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
    }

    public void OnClick()
    {
        if (isOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;
        ResetCts();
        OpenArchiveCor(cts.Token).Forget();
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        ResetCts();
        CloseArchiveCor(cts.Token).Forget();
    }

    // 진행 중이던 애니메이션 취소 + 새 토큰 발급
    private void ResetCts()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = new CancellationTokenSource();
    }

    private async UniTask OpenArchiveCor(CancellationToken token)
    {
        archiveUI.SetActive(true);
        float t = Progress01(archiveUI.transform.localScale); // 현재 스케일에서 이어서 열기(연타 시 튐 방지)
        float speed = 5f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * speed;
            archiveUI.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
            await UniTask.Yield(token);
        }
    }

    private async UniTask CloseArchiveCor(CancellationToken token)
    {
        float t = 1f - Progress01(archiveUI.transform.localScale);
        float speed = 5f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * speed;
            archiveUI.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
            await UniTask.Yield(token);
        }
        archiveUI.SetActive(false);
    }

    // 현재 스케일이 0~1 열림 진행도의 어디쯤인지(연타/중간취소 시 이어서 애니메이션)
    private static float Progress01(Vector3 scale)
        => Mathf.Clamp01(scale.x);
}
