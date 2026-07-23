using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class ProductionFacility : MonoBehaviour, IPlaceAble
{
    [SerializeField] private ProductionValue basicValue;
    private int productAmount;
    private int workerAmount = 0;
    private int maxWorker;
    private ResourcesManager resourcesManager;
    private BuildingPool buildingPool;
    private CitizenManager citizenManager;
    private FacilityManager facilityManager;
    public ProductionType ProductionType => basicValue.Type;

    public event Action OnWorkerChanged;
    public event Action OnBreak;
    public event Action OnResur;
    private MapBoard board;

    public ProductionValue BasicValue => basicValue;

    public MapBoard Board => board;

    private int maxUpgrade = 4;
    private int upgradeCount = 1;
    private int amountUpgrade = 0;
    private int citizenUpgrade = 0;
    public int WorkerAmount => workerAmount;
    public int MaxWorker => maxWorker;
    public int ProductAmount => productAmount;
    private Dictionary<ProductionType, int> upgradeCostCopy = new();
    public Dictionary<ProductionType, int> UpgradeCostCopy => upgradeCostCopy;
    public int UpgradeCount => upgradeCount;

    [Inject]
    private void Construct(ResourcesManager resourcesManager, CitizenManager citizenManager, BuildingPool buildingPool, FacilityManager facilityManager)
    {
        this.resourcesManager = resourcesManager;
        this.citizenManager = citizenManager;
        this.buildingPool = buildingPool;
        this.facilityManager = facilityManager;
    }

    public void Init()
    {
        productAmount = basicValue.DefaultAmount + amountUpgrade * 10;
        maxWorker = basicValue.DefaultMaxWorker + citizenUpgrade;
        workerAmount = 0;
        upgradeCostCopy = BasicValue.UpgradeCost;

        resourcesManager.ProductChanged(basicValue.ConstructProduct);
        facilityManager.AddFacility(this);
    }

    private void OnDisable()
    {
        ReleaseAllWorkers();
        if(facilityManager != null)
            facilityManager.RemoveFacility(this);
    }

    private void ReleaseAllWorkers()
    {
        while (workerAmount > 0)
        {
            workerAmount--;
            citizenManager.RecycleCitizen();
        }
        UpdateInfo();
    }

    public void Release()
    {
        var refund = new System.Collections.Generic.Dictionary<ProductionType, int>();
        foreach (var kv in basicValue.ConstructProduct)
        {
            refund[kv.Key] = -kv.Value;
        }
        resourcesManager.ProductChanged(refund);
        buildingPool.Return(gameObject);
    }

    public void IncreaseWorker()
    {
        if (citizenManager == null) return;

        if (workerAmount < maxWorker && citizenManager.CheckCanUseCitizen())
        {
            workerAmount++;
            UpdateInfo();
            citizenManager.UseCitizen();
        }
    }

    public void DecreaseWorker()
    {
        if (citizenManager == null) return;

        if (workerAmount > 0)
        {
            workerAmount--;
            UpdateInfo();
            citizenManager.RecycleCitizen();
        }
    }

    public void UpdateInfo()
    {
        OnWorkerChanged?.Invoke();
    }

    public (ProductionType, int) ProduceProduction()
    {
        return (basicValue.Type, productAmount * workerAmount);
    }

    public void SetBoard(MapBoard board)
    {
        this.board = board;
    }

    public void Upgrade()
    {
        upgradeCount++;
        if(upgradeCount % 5 == 0)
        {
            Debug.Log("특수 자원 생산 시작");
        }
        else if(upgradeCount % 2 == 0)
        {
            amountUpgrade++;
            productAmount += amountUpgrade * 10;
        }
        else
        {
            citizenUpgrade++;
            maxWorker += citizenUpgrade;
        }

        resourcesManager.ProductChanged(upgradeCostCopy);

        foreach (var key in new List<ProductionType>(upgradeCostCopy.Keys))
        {
            upgradeCostCopy[key] = basicValue.UpgradeCost[key] * upgradeCount;
        }

        UpdateInfo();
    }

    public bool CheckCanUpgrade()
    {
        return upgradeCount <= maxUpgrade && resourcesManager.CheckResources(upgradeCostCopy);
    }

    //건물 마다 업그레이드
    //생산 건물 타입 마다 업그레이드
    //디펜스동안은 건물 마다 업그레이드 단 상한선 존재
    //게임 끝나고 얻은 포인트로 생산 건물 타입의 업그레이드
    // - 건물 업그레이드 최대 횟수 증가
    // - 건물 건설 비용 감소
    // - 건물 업그레이드 비용 감소
    //건물 마다 업그레이드
    // - 생산량 증가
    // - 배치 가능한 시민 수 증가
    // - 특수 자원 생산 가능(업그레이드 일정 횟수 이상부터)
}
