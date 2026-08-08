using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HeroUpgrade
{
    public int level;
    public int maxLevel;
    public List<ResourceCost> resourceCost;
    public float attackUP;
    public float defenseUP;
    public float healthUP;
    public float attackSpeedUP;
    public void Upgrade(Hero hero)
    {
        if (level < maxLevel)
        {
            level++;

        }
    }
    public void Reset()
    {
        level = 0;
    }
}

public class HeroUpgradeManager : MonoBehaviour
{
    [SerializeField] private List<HeroUpgrade> heroUpgrades = new();


}
