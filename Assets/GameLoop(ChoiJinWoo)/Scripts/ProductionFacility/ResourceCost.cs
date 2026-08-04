using System;
using System.Collections.Generic;

[Serializable]
public struct ResourceCost
{
    public ProductionType Type;
    public int Amount;
}

// 저작용 List<ResourceCost>를 런타임에서 쓰는 (ProductionType, int)[] 비용 배열로 바꾸는 변환 하나만 모아둔다.
// HouseConfig/ProductionValue가 저마다 같은 반복문을 복붙해서 쓰고 있었다.
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
}
