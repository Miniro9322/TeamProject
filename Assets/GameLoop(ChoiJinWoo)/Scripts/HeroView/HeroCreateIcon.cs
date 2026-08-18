using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 생성 버튼 하나(근접용 인스턴스 1개, 원거리용 인스턴스 1개). 아이콘/라벨은 인스펙터에서 미리 세팅.
public class HeroCreateIcon : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private List<ResourceCost> resourceCost;
    [SerializeField] private List<ResourceIcon> resourceIcons;
    [SerializeField] private string createName;
    [SerializeField] private HeroCreateToolTipTrigger toolTipTrigger;
    public List<ResourceCost> ResourceCost => resourceCost;
    public List<ResourceIcon> ResourceIcons => resourceIcons;
    public string CreateName => createName;

    private void Awake()
    {
        if (toolTipTrigger != null) toolTipTrigger.SetData(createName, resourceCost, resourceIcons);
    }

    public void Set(bool interactable, Action onClick)
    {
        button.interactable = interactable;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke());
    }

    public void SetInteractable(bool interactable)
    {
        button.interactable = interactable;
    }
}
