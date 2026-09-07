using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class FacilityManager
{
    private List<ProductionFacility> facilities = new();
    private List<(ProductionType Type, int Amount)> products = new();

    private ResourcesManager resourcesManager;
    private EnviromentManager enviromentManager;
    [Inject]
    private void Construct(ResourcesManager resourcesManager, EnviromentManager enviromentManager)
    {
        this.resourcesManager = resourcesManager;
        this.enviromentManager = enviromentManager;

        enviromentManager.OnDay += SumProduct;
    }   

    public void AddFacility(ProductionFacility facility)
    {
        facilities.Add(facility);
    }

    public void RemoveFacility(ProductionFacility facility)
    {
        facilities.Remove(facility);
    }

    public void SumProduct()
    {
        var totals = GetTotalProduction();
        resourcesManager.ProductChanged(totals);
        products.Clear();
    }

    public (ProductionType Type, int Amount)[] GetTotalProduction()
    {
        products.Clear();
        foreach (var facility in facilities)
        {
            var product = facility.ProduceProduction();
            if (product == default)
                continue;

            int index = products.FindIndex(p => p.Type == product.Item1);
            if (index < 0)
            {
                products.Add((product.Item1, product.Item2));
            }
            else
            {
                products[index] = (product.Item1, products[index].Amount + product.Item2);
            }
        }

        return products.ToArray();
    }
}
