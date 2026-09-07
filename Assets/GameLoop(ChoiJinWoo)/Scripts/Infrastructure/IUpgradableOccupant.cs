using System;

public interface IUpgradableOccupant
{
    int UpgradeCount { get; }
    string NextUpgradeInfo { get; }
    int MaxUpgrade { get; }
    (ProductionType Type, int Amount)[] UpgradeCostCopy { get; }
    event Action Changed;
    bool CheckCanUpgrade();
    void Upgrade();
}
