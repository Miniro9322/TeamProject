using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

// HeroCreateIcon이 자기 데이터(이름/자원 비용/자원 아이콘)를 SetData로 밀어넣으면, 마우스 호버 시
// HeroCreateToolTipUI에 그 값을 넘겨 띄운다. TooltipTrigger와 같은 호버 딜레이 방식.
public class HeroCreateToolTipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private string createName;
    private List<ResourceCost> resourceCost;
    private List<ResourceIcon> resourceIcons;
    private float hoverDelay = 0.2f;

    private CancellationTokenSource cts;

    public void SetData(string createName, List<ResourceCost> resourceCost, List<ResourceIcon> resourceIcons)
    {
        this.createName = createName;
        this.resourceCost = resourceCost;
        this.resourceIcons = resourceIcons;
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
        HeroCreateToolTipUI.Instance.Hide();
    }

    private void OnDisable()
    {
        // 호버 도중 버튼/패널이 꺼지면 PointerExit이 안 불릴 수 있어 여기서도 닫아준다.
        cts?.Cancel();
        if (HeroCreateToolTipUI.Instance != null) HeroCreateToolTipUI.Instance.Hide();
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

        HeroCreateToolTipUI.Instance.Show(createName, resourceCost, resourceIcons);
    }
}
