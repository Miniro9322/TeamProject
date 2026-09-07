using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ProductionEconomyConfig", menuName = "Scriptable Objects/ProductionEconomyConfig")]
public class ProductionEconomyConfig : ScriptableObject
{
    [SerializeField] private List<BaseUpgradeData> constructCostUpgrades;
    [SerializeField] private List<BaseUpgradeData> upgradeCostUpgrades;
    [SerializeField] private List<BaseUpgradeData> productAmountUpgrades;

    public IReadOnlyList<BaseUpgradeData> ConstructCostUpgrades => constructCostUpgrades;
    public IReadOnlyList<BaseUpgradeData> UpgradeCostUpgrades => upgradeCostUpgrades;
    public IReadOnlyList<BaseUpgradeData> ProductAmountUpgrades => productAmountUpgrades;
}
