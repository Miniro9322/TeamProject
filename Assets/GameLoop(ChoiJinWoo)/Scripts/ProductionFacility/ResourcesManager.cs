using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class ResourcesManager : MonoBehaviour
{
    [SerializeField] private int wood = 500;
    [SerializeField] private int food = 500;
    [SerializeField] private int gold = 500;
    [SerializeField] private int iron = 500;
    [SerializeField] private int stone = 500;
    [SerializeField] private int special = 0;
    [SerializeField] private int tradeAmount = 100;
    [SerializeField] private List<BaseUpgradeData> startingResourceUpgrades;

    public int Wood => wood;
    public int Food => food;
    public int Gold => gold;
    public int Iron => iron;
    public int Stone => stone;
    public int Special => special;

    public int TradeAmount => tradeAmount;

    public event Action ProductUpdate;

    private int initialWood, initialFood, initialGold, initialIron, initialStone, initialSpecial;

    [Inject]
    private void Construct(UpgradeState upgradeState)
    {
        int bonus = (int)upgradeState.GetTotalEffect(startingResourceUpgrades);
        wood += bonus;
        food += bonus;
        gold += bonus;
        iron += bonus;
        stone += bonus;

        initialWood = wood;
        initialFood = food;
        initialGold = gold;
        initialIron = iron;
        initialStone = stone;
        initialSpecial = special;
    }

    public void Reset()
    {
        wood = initialWood;
        food = initialFood;
        gold = initialGold;
        iron = initialIron;
        stone = initialStone;
        special = initialSpecial;

        ProductUpdate?.Invoke();
    }

    private void Start()
    {
        ProductUpdate?.Invoke();
    }

    public void ProductChanged((ProductionType Type, int Amount)[] products)
    {
        foreach (var product in products)
        {
            Resource(product.Type) += product.Amount;
        }

        ProductUpdate?.Invoke();
    }

    public bool CheckResources((ProductionType Type, int Amount)[] resources)
    {
        foreach (var resource in resources)
        {
            if (-resource.Amount > Resource(resource.Type)) return false;
        }

        return true;
    }

    public int GetAmount(ProductionType type) => Resource(type);

    private ref int Resource(ProductionType type)
    {
        switch (type)
        {
            case ProductionType.Wood: return ref wood;
            case ProductionType.Food: return ref food;
            case ProductionType.Gold: return ref gold;
            case ProductionType.Iron: return ref iron;
            case ProductionType.Stone: return ref stone;
            default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public void TradeResource(ProductionType type)
    {
        special--;
        Resource(type) += tradeAmount;

        ProductUpdate?.Invoke();
    }

    public void GetSpecial()
    {
        special++;

        ProductUpdate?.Invoke();
    }

    public void RestoreResources(int wood, int food, int gold, int iron, int stone, int special)
    {
        this.wood = wood;
        this.food = food;
        this.gold = gold;
        this.iron = iron;
        this.stone = stone;
        this.special = special;

        ProductUpdate?.Invoke();
    }
}
