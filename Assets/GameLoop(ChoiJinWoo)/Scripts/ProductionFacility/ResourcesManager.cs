using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class ResourcesManager : MonoBehaviour
{
    [Header("시작 자원량 - 자원 종류를 추가할 땐 여기 항목만 추가하면 된다")]
    [SerializeField] private List<ResourceCost> startingAmounts = new()
    {
        new ResourceCost { Type = ProductionType.Wood,  Amount = 500 },
        new ResourceCost { Type = ProductionType.Food,  Amount = 500 },
        new ResourceCost { Type = ProductionType.Gold,  Amount = 500 },
        new ResourceCost { Type = ProductionType.Iron,  Amount = 500 },
        new ResourceCost { Type = ProductionType.Stone, Amount = 500 },
    };
    [SerializeField] private int special = 0;
    [SerializeField] private int tradeAmount = 100;
    [SerializeField] private List<BaseUpgradeData> startingResourceUpgrades;

    // ProductionType -> 보유량. ref int + 타입별 switch 를 대체한다.
    private readonly Dictionary<ProductionType, int> amounts = new();
    private readonly Dictionary<ProductionType, int> initialAmounts = new();
    private int initialSpecial;

    public int Wood => GetAmount(ProductionType.Wood);
    public int Food => GetAmount(ProductionType.Food);
    public int Gold => GetAmount(ProductionType.Gold);
    public int Iron => GetAmount(ProductionType.Iron);
    public int Stone => GetAmount(ProductionType.Stone);
    public int Special => special;

    public int TradeAmount => tradeAmount;

    public event Action ProductUpdate;

    [Inject]
    private void Construct(UpgradeState upgradeState)
    {
        int bonus = (int)upgradeState.GetTotalEffect(startingResourceUpgrades);

        amounts.Clear();
        foreach (var entry in startingAmounts)
            amounts[entry.Type] = entry.Amount + bonus;

        initialAmounts.Clear();
        foreach (var pair in amounts)
            initialAmounts[pair.Key] = pair.Value;
        initialSpecial = special;
    }

    public void Reset()
    {
        foreach (var pair in initialAmounts)
            amounts[pair.Key] = pair.Value;
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
            amounts[product.Type] = GetAmount(product.Type) + product.Amount;

        ProductUpdate?.Invoke();
    }

    public bool CheckResources((ProductionType Type, int Amount)[] resources)
    {
        foreach (var resource in resources)
        {
            if (-resource.Amount > GetAmount(resource.Type)) return false;
        }

        return true;
    }

    public int GetAmount(ProductionType type) => amounts.TryGetValue(type, out int value) ? value : 0;

    public void TradeResource(ProductionType type)
    {
        special--;
        amounts[type] = GetAmount(type) + tradeAmount;

        ProductUpdate?.Invoke();
    }

    public void GetSpecial()
    {
        special++;

        ProductUpdate?.Invoke();
    }

    public void RestoreResources(int wood, int food, int gold, int iron, int stone, int special)
    {
        amounts[ProductionType.Wood] = wood;
        amounts[ProductionType.Food] = food;
        amounts[ProductionType.Gold] = gold;
        amounts[ProductionType.Iron] = iron;
        amounts[ProductionType.Stone] = stone;
        this.special = special;

        ProductUpdate?.Invoke();
    }
}
