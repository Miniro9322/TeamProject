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

    [Inject]
    private void Construct(UpgradeState upgradeState)
    {
        int bonus = (int)upgradeState.GetTotalEffect(startingResourceUpgrades);
        wood += bonus;
        food += bonus;
        gold += bonus;
        iron += bonus;
        stone += bonus;
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

    // 자원 종류 하나당 필드 하나씩 직렬화해야 해서(Dictionary는 인스펙터 지원이 없음) 필드는 그대로 두고,
    // 타입 -> 필드 매핑만 한 곳에 모아 ProductChanged/CheckResources의 중복 switch를 없앤다.
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
        Debug.Log("trade 호출됨");

        switch (type)
        {
            case ProductionType.Wood:
                special--;
                wood += tradeAmount;
                break;
            case ProductionType.Food:
                special--;
                food += tradeAmount;
                break;
            case ProductionType.Gold:
                special--;
                gold += tradeAmount;
                break;
            case ProductionType.Iron:
                special--;
                iron += tradeAmount;
                break;
            case ProductionType.Stone:
                special--;
                stone += tradeAmount;
                break;
            default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        ProductUpdate?.Invoke();
    }

    public void GetSpecial()
    {
        special++;

        ProductUpdate?.Invoke();
    }

    // 세이브 데이터로 자원 6종을 그대로 덮어쓴다 (로드 복원 전용)
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
