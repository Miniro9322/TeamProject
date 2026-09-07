using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HeroCreateIcon : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private List<ResourceCost> resourceCost;
    [SerializeField] private List<ResourceIcon> resourceIcons;
    [SerializeField] private string createName;
    [SerializeField] private Image activeFrame;
    public List<ResourceCost> ResourceCost => resourceCost;
    public List<ResourceIcon> ResourceIcons => resourceIcons;
    public string CreateName => createName;
    private UnityAction currentListener;
    public void Set(bool interactable, Action onClick)
    {
        button.interactable = interactable;
        if (currentListener != null) button.onClick.RemoveListener(currentListener);
        currentListener = () => onClick?.Invoke();
        button.onClick.AddListener(currentListener);
    }

    public void SetInteractable(bool interactable)
    {
        button.interactable = interactable;
    }

    public void SetSelected(bool selected)
    {
        activeFrame.enabled = selected;
    }
}
