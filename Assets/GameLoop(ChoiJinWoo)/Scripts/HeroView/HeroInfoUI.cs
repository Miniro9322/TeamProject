using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeroInfoUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI heroInfoText;
    [SerializeField] private Image heroIcon;
    [SerializeField] private Button button;

    public void Set(Placeable slot, Action<Placeable> onclick)
    {
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onclick?.Invoke(slot));
        var temp = slot.prefab.GetComponent<Hero>();

        var sb = new StringBuilder();

        heroInfoText.text = sb.ToString();
    }
}
