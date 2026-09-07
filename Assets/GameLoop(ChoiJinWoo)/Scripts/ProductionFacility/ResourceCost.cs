using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct ResourceCost
{
    public ProductionType Type;
    public int Amount;
}

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

    public static (ProductionType Type, int Amount)[] BuildRefundWith(
        this (ProductionType Type, int Amount)[] constructPaid,
        (ProductionType Type, int Amount)[] upgradeSpent)
    {
        var refund = new (ProductionType Type, int Amount)[constructPaid.Length + upgradeSpent.Length];
        for (int i = 0; i < constructPaid.Length; i++)
        {
            refund[i] = (constructPaid[i].Type, -constructPaid[i].Amount);
        }
        for (int i = 0; i < upgradeSpent.Length; i++)
        {
            refund[constructPaid.Length + i] = (upgradeSpent[i].Type, -upgradeSpent[i].Amount);
        }
        return refund;
    }

    public static void AccumulateInto(this (ProductionType Type, int Amount)[] cost, (ProductionType Type, int Amount)[] totalSpent)
    {
        for (int i = 0; i < totalSpent.Length; i++)
        {
            totalSpent[i] = (totalSpent[i].Type, totalSpent[i].Amount + cost[i].Amount);
        }
    }
}
