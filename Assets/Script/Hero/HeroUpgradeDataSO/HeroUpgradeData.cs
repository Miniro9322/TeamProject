using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "HeroUpgradeData", menuName = "UpgradeData/HeroUpgradeData")]
public class HeroUpgradeData : ScriptableObject
{
    public List<AttackDataSO> attackDatas;
    public List<AttackSelectorSO> selectors;
    public List<AttackProcSO> procs;

    public void Upgrade(Hero hero)
    {
        hero.ExchangeAttackDatas(attackDatas, selectors, procs);
    }
}
