using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HeroDescItem : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI descText;

    public void SetUp(AttackDescription desc)
    {
        icon.sprite = desc.icon;
        descText.text = DataTableManager.StringTable.Get(desc.attackDescriptionKey);
    }
}
