using System;
using UnityEngine;

public class ProductionFacility : MonoBehaviour
{
    [SerializeField] ProductionValue basicValue;
    private int productAmount;
    private bool isCrashed = false;
    private int workerAmount = 0;
    private int maxWorker;

    public event Action<ProductionType ,int> Produce;

    private void Awake()
    {
        productAmount = basicValue.DefaultAmount;
        maxWorker = basicValue.DefaultMaxWorker;
    }

    public void IncreasWorker()
    {
        if(workerAmount < maxWorker)
        {
            workerAmount++;
        }
    }

    public void DecreaseWorker()
    {
        if(workerAmount > 0)
        {
            workerAmount--;
        }
    }

    public void ProduceProduction()
    {
        if (!isCrashed)
        {
            Produce?.Invoke(basicValue.Type, productAmount * workerAmount);
        }
    }
}
