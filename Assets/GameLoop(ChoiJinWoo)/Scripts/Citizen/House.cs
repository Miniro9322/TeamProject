using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class House : MonoBehaviour, IPlaceAble
{
    [SerializeField] private int maxCitizenAmount;
    [Header("건설에 필요한 자원")]
    [SerializeField] private List<ResourceCost> cost;
    [Header("건물 이름")]
    [SerializeField] private string houseName;
    [Header("건물 설명")]
    [SerializeField] private string houseInfo;
    private CitizenManager manager;
    private ResourcesManager resourcesManager;
    private MapBoard board;
    public (ProductionType Type, int Amount)[] Resources
    {
        get
        {
            var temp = new (ProductionType, int)[cost.Count];

            for (int i = 0; i < cost.Count; i++)
            {
                temp[i] = (cost[i].Type, -cost[i].Amount);
            }

            return temp;
        }
    }

    public MapBoard Board => board;
    public string HouseName => houseName;
    public string HouseInfo => houseInfo;

    public event Action OnBreak;
    public event Action OnResur;

    public void OnDestroy()
    {
        var resources = Resources;
        var refund = new (ProductionType Type, int Amount)[resources.Length];
        for (int i = 0; i < resources.Length; i++)
        {
            refund[i] = (resources[i].Type, -resources[i].Amount);
        }

        resourcesManager.ProductChanged(refund);
    }

    public void SetBoard(MapBoard board)
    {
        this.board = board;
    }

    [Inject]
    private void Construct(CitizenManager manager)
    {
        this.manager = manager;
    }

    [Inject]
    private void Construct(ResourcesManager resourcesManager)
    {
        this.resourcesManager = resourcesManager;
    }

    private void Start()
    {
        manager.IncreaseMaxCitizen(maxCitizenAmount);
        resourcesManager.ProductChanged(Resources);
    }
}
