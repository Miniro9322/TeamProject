using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeroUpgradePanel : MonoBehaviour
{
    private Hero hero;
    private HeroRosterIcon currentIcon;
    public void InitHeroInfo(Hero hero)
    {
        this.hero = hero;
    }

    public void SkillUpgradeHero()
    {
        hero.SkillUpgrade();
        currentIcon.UpdateLevel(hero);
    }

    public void StatUpgradeHero()
    {
        hero.StatUpgrade();
        currentIcon.UpdateLevel(hero);
    }

    //public void SkillUpgradeHero(HeroRosterIcon icon)
    //{
    //    SkillUpgradeHero();
    //}

    //public void StatUpgradeHero(HeroRosterIcon icon)
    //{
    //    StatUpgradeHero();
    //}
    public void PositionAtIconY(HeroRosterIcon icon)
    {
        RectTransform rt = (RectTransform)transform;
        Vector3 pos = rt.position;
        pos.y = ((RectTransform)icon.transform).position.y;
        rt.position = pos;
        currentIcon = icon;
    }

}
