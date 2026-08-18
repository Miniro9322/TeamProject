using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class HeroUpgradeResourcesUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI amount;

    public void SetIcon(Sprite icon)
    {
        this.icon.sprite = icon;
    }
    public void SetAmount(int amount)
    {
        this.amount.text = amount.ToString();
    }
}
