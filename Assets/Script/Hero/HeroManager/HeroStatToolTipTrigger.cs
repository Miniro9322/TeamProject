using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class HeroStatToolTipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private float hoverDelay = 0.2f;
    private HeroRosterEntry entry;
    private CancellationTokenSource cts;

    public void SetData(HeroRosterEntry entry)
    {
        this.entry = entry;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        cts?.Cancel();
        cts = new CancellationTokenSource();
        DelayedShow(cts.Token).Forget();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        cts?.Cancel();
        HeroStatToolTipUI.Instance.Hide();
    }

    private void OnDisable()
    {
        // 호버 도중 버튼/패널이 꺼지면 PointerExit이 안 불릴 수 있어 여기서도 닫아준다.
        cts?.Cancel();
        if (HeroStatToolTipUI.Instance != null) HeroStatToolTipUI.Instance.Hide();
    }

    private async UniTaskVoid DelayedShow(CancellationToken token)
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(hoverDelay), cancellationToken: token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        HeroStatToolTipUI.Instance.Show(entry, Mouse.current.position.ReadValue());
    }
}
