using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 생성 버튼 하나(근접용 인스턴스 1개, 원거리용 인스턴스 1개). 아이콘/라벨은 인스펙터에서 미리 세팅.
public class HeroCreateIcon : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI text;

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
