using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeroUpgradePanel : MonoBehaviour
{
    private Hero hero;
    private HeroRosterIcon currentIcon;
    private ClickOutsideCloser outsideCloser;

    public void InitHeroInfo(Hero hero)
    {
        this.hero = hero;
    }

    public void SkillUpgradeHero()
    {
        hero.SkillUpgrade();
        currentIcon.UpdateLevel(hero.StatLevel, hero.SkillLevel);
    }

    public void StatUpgradeHero()
    {
        hero.StatUpgrade();
        currentIcon.UpdateLevel(hero.StatLevel, hero.SkillLevel);
    }

    private void Update()
    {
        if (outsideCloser != null && outsideCloser.ClickedOutside())
            gameObject.SetActive(false);
    }

    //public void SkillUpgradeHero(HeroRosterIcon icon)
    //{
    //    SkillUpgradeHero();
    //}

    //public void StatUpgradeHero(HeroRosterIcon icon)
    //{
    //    StatUpgradeHero();
    //}
    // rosterContainer: 이 패널을 연 로스터 아이콘들의 부모. alsoSelf로 넘겨야 로스터 아이콘 클릭(다른
    // 영웅으로 갈아타기)이 "바깥 클릭"으로 오판되어 패널이 열리자마자 다시 닫히는 걸 막을 수 있다.
    public void PositionAtIconY(HeroRosterIcon icon, Transform rosterContainer)
    {
        RectTransform rt = (RectTransform)transform;
        Vector3 pos = rt.position;
        pos.x = ((RectTransform)icon.transform).position.x;
        rt.position = pos;
        currentIcon = icon;

        outsideCloser ??= new ClickOutsideCloser((RectTransform)transform, rosterContainer);
        outsideCloser.MarkOpened();
    }

}
