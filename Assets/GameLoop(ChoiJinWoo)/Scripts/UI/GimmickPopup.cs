using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class GimmickPopup : MonoBehaviour
{
    private const float HoldSeconds = 3f;

    private static readonly int InHash = Animator.StringToHash("GimmickIn");
    private static readonly int OutHash = Animator.StringToHash("GimmickOut");

    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descText;

    private Animator slideAnim;
    private CancellationTokenSource closeTimer;

    private void Awake()
    {
        slideAnim = GetComponent<Animator>();
    }

    public void Show(string nameKey, string descKey)
    {
        nameText.text = DataTableManager.StringTable.Get(nameKey);
        descText.text = DataTableManager.StringTable.Get(descKey);
        Show();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        slideAnim.Play(InHash, 0, 0f);
        RestartCloseTimer();
    }

    private void ClosePanel()
    {
        slideAnim.Play(OutHash, 0, 0f);
    }

    public void FinishClose()
    {
        gameObject.SetActive(false);
    }

    private void RestartCloseTimer()
    {
        CancelCloseTimer();
        closeTimer = new CancellationTokenSource();
        WaitAndCloseAsync(closeTimer.Token).Forget();
    }

    private void CancelCloseTimer()
    {
        if (closeTimer == null)
        {
            return;
        }
        closeTimer.Cancel();
        closeTimer.Dispose();
        closeTimer = null;
    }

    private async UniTaskVoid WaitAndCloseAsync(CancellationToken token)
    {
        bool cancelled = await UniTask.Delay(TimeSpan.FromSeconds(HoldSeconds), cancellationToken: token).SuppressCancellationThrow();
        if (cancelled)
        {
            return;
        }
        ClosePanel();
    }

    private void OnDestroy()
    {
        CancelCloseTimer();
    }
}
