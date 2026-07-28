using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "HeroUpgradeData", menuName = "UpgradeData/HeroUpgradeData")]
public class HeroUpgradeData : ScriptableObject
{
    public List<AttackDataSO> attackDatas;
    public List<AttackSelectorSO> selectors;
    public List<AttackProcSO> procs;

    [SerializeField] private List<ProductionType> costType;
    [SerializeField] private List<int> costAmount;

    public Dictionary<ProductionType, int> Cost
    {
        get
        {
            if (costType.Count != costAmount.Count)
            {
                return null;
            }
            else
            {
                Dictionary<ProductionType, int> temp = new();

                for (int i = 0; i < costType.Count; i++)
                {
                    temp[costType[i]] = -costAmount[i];
                }

                return temp;
            }
        }
    }

    public void Upgrade(Hero hero)
    {

        hero.ExchangeAttackDatas(attackDatas, selectors, procs);
    }
}
