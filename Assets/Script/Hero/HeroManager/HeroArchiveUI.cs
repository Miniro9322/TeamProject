using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeroArchiveUI : MonoBehaviour
{
    [SerializeField] private HeroArchiveItem itemPrefab;
    [SerializeField] private HeroRegistry registry;
    [SerializeField] private Transform listContent;

    [SerializeField] private Image mainImage;
    [SerializeField] private TextMeshProUGUI nameText;
    private Animator bookAnimator;

    private void OnEnable()
    {
        BuildList();
    }

    private void OnDisable()
    {
        ClearList();
    }
    private void BuildList()
    {
        ClearList();
        foreach (HeroData data in registry.AllHeroDatas)
        {
            HeroArchiveItem item = Instantiate(itemPrefab, listContent);
            item.Setup(data, OnHeroArchiveClicked);
        }
    }

    private void OnHeroArchiveClicked(HeroData picked)
    {
        mainImage.sprite = picked.Icon;
        nameText.text = DataTableManager.StringTable.Get(picked.HeroNameKey);
    }
    private void ClearList()
    {
        for (int i = listContent.childCount - 1; i >= 0; i--)
            Destroy(listContent.GetChild(i).gameObject);
    }

}
