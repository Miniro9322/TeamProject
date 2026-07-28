using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeroUpgradePanel : MonoBehaviour
{
    private Hero hero;

    public void InitHeroInfo(Hero hero)
    {
        this.hero = hero;
    }

    public void SkillUpgradeHero()
    {
        hero.SkillUpgrade();
    }

    public void StatUpgradeHero()
    {
        hero.StatUpgrade();
    }
    public void PositionAtIconY(RectTransform anchor)
    {
        RectTransform rt = (RectTransform)transform;
        Vector3 pos = rt.position;
        pos.y = anchor.position.y;
        rt.position = pos;
    }

}
