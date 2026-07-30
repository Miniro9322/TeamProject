using System;
using UnityEngine;

public class ResourcesManager : MonoBehaviour
{
    [SerializeField] private int wood = 500;
    [SerializeField] private int food = 500;
    [SerializeField] private int gold = 500;
    [SerializeField] private int iron = 500;
    [SerializeField] private int stone = 500;

    public int Wood => wood;
    public int Food => food;
    public int Gold => gold;
    public int Iron => iron;
    public int Stone => stone;

    public event Action ProductUpdate;

    private void Start()
    {
        ProductUpdate?.Invoke();
    }

    public void ProductChanged((ProductionType Type, int Amount)[] products)
    {
        foreach (var product in products)
        {
            switch (product.Type)
            {
                case ProductionType.Wood: wood += product.Amount; break;
                case ProductionType.Food: food += product.Amount; break;
                case ProductionType.Gold: gold += product.Amount; break;
                case ProductionType.Iron: iron += product.Amount; break;
                case ProductionType.Stone: stone += product.Amount; break;
            }
        }

        ProductUpdate?.Invoke();
    }

    public bool CheckResources((ProductionType Type, int Amount)[] resources)
    {
        foreach (var resource in resources)
        {
            switch (resource.Type)
            {
                case ProductionType.Wood:
                    if (-resource.Amount > wood) return false;
                    break;
                case ProductionType.Stone:
                    if (-resource.Amount > stone) return false;
                    break;
                case ProductionType.Gold:
                    if (-resource.Amount > gold) return false;
                    break;
                case ProductionType.Iron:
                    if (-resource.Amount > iron) return false;
                    break;
                case ProductionType.Food:
                    if (-resource.Amount > food) return false;
                    break;
            }
        }

        return true;
    }
}
