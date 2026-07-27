using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 생성 가능한 영웅 슬롯 하나를 표시하는 버튼. HeroSetPanel이 슬롯 하나당 하나씩 인스턴스화한다.
public class HeroCreateIcon : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI text;

    public void Set(Placeable slot, bool interactable, Action<Placeable> onClick, string label)
    {
        icon.sprite = slot.icon;
        button.interactable = interactable;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke(slot));
        text.text = label;
    }

    public void SetInteractable(bool interactable)
    {
        button.interactable = interactable;
    }
}
