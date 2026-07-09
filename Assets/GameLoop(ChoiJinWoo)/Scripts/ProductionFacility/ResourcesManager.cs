using System;
using System.Collections.Generic;
using UnityEngine;

public class ResourcesManager : MonoBehaviour
{
    [SerializeField] private int wood = 500;
    [SerializeField] private int food = 500;
    [SerializeField] private int gold = 500;
    [SerializeField] private int iron = 500;
    [SerializeField] private int stone = 500;

    public event Action<ProductionType, int> ProductUpdate;

    public void ProductChanged(ProductionType type, int amount)
    {
        switch (type)
        {
            case ProductionType.Wood:
                if(wood + amount < 0)
                {
                    Debug.Log("건설 불가능");
                    return;
                }
                wood += amount;
                break;
            case ProductionType.Food:
                if (food + amount < 0)
                {
                    Debug.Log("건설 불가능");
                    return;
                }
                food += amount;
                break;
            case ProductionType.Gold:
                if (gold + amount < 0)
                {
                    Debug.Log("건설 불가능");
                    return;
                }
                gold += amount;
                break;
            case ProductionType.Iron:
                if (iron + amount < 0)
                {
                    Debug.Log("건설 불가능");
                    return;
                }
                iron += amount;
                break;
            case ProductionType.Stone:
                if (stone + amount < 0)
                {
                    Debug.Log("건설 불가능");
                    return;
                }
                stone += amount;
                break;
        }

        Debug.Log($"목재: {wood}, 식량: {food}, 금: {gold}, 철재: {iron}, 석재: {stone}");

        ProductUpdate?.Invoke(type, amount);
    }

    public bool TryBuild(List<ProductionType> types, List<int> amounts)
    {
        foreach(var type in types)
        {
            switch (type)
            {
                case ProductionType.Wood:
                    return wood - amounts[types.IndexOf(type)] < 0 ? false : true;
                case ProductionType.Food:
                    return food - amounts[types.IndexOf(type)] < 0 ? false : true;
                case ProductionType.Gold:
                    return gold - amounts[types.IndexOf(type)] < 0 ? false : true;
                case ProductionType.Iron:
                    return iron - amounts[types.IndexOf(type)] < 0 ? false : true;
                case ProductionType.Stone:
                    return stone - amounts[types.IndexOf(type)] < 0 ? false : true;
            }
        }

        return true;
    }
}
