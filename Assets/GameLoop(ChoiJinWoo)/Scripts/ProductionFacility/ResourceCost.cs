using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct ResourceCost
{
    public ProductionType Type;
    public int Amount;
}

// 저작용 List<ResourceCost>를 런타임에서 쓰는 (ProductionType, int)[] 비용 배열로 바꾸는 변환과,
// 그 비용 배열에 할인을 적용하는 계산을 모아둔다. ProductionFacility/House가 저마다 같은 반복문을
// 복붙해서 쓰고 있었다.
public static class ResourceCostExtensions
{
    public static (ProductionType Type, int Amount)[] ToNegatedCostArray(this List<ResourceCost> costs)
    {
        var result = new (ProductionType Type, int Amount)[costs.Count];
        for (int i = 0; i < costs.Count; i++)
        {
            result[i] = (costs[i].Type, -costs[i].Amount);
        }
        return result;
    }

    public static (ProductionType Type, int Amount)[] ApplyDiscount(this (ProductionType Type, int Amount)[] cost, float discount)
    {
        var result = new (ProductionType, int)[cost.Length];
        for (int i = 0; i < cost.Length; i++)
        {
            result[i] = (cost[i].Type, Mathf.RoundToInt(cost[i].Amount * (1f - discount)));
        }
        return result;
    }

    public static (ProductionType Type, int Amount)[] Multiply(this (ProductionType Type, int Amount)[] cost, int n)
    {
        var result = new (ProductionType, int)[cost.Length];
        for (int i = 0; i < cost.Length; i++)
        {
            result[i] = (cost[i].Type, cost[i].Amount * n);
        }
        return result;
    }

    public static (ProductionType Type, int Amount)[] Scale(this (ProductionType Type, int Amount)[] cost, float factor)
    {
        var result = new (ProductionType, int)[cost.Length];
        for (int i = 0; i < cost.Length; i++)
        {
            result[i] = (cost[i].Type, Mathf.RoundToInt(cost[i].Amount * factor));
        }
        return result;
    }

    public static (ProductionType Type, int Amount)[] Add(this (ProductionType Type, int Amount)[] a, (ProductionType Type, int Amount)[] b)
    {
        var result = new (ProductionType, int)[a.Length];
        for (int i = 0; i < a.Length; i++)
        {
            result[i] = (a[i].Type, a[i].Amount + b[i].Amount);
        }
        return result;
    }
}
