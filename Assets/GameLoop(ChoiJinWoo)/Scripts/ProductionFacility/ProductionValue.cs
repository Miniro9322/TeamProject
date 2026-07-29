using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ProductionValue", menuName = "Scriptable Objects/ProductionValue")]
public class ProductionValue : ScriptableObject
{
    [Header("생산하는 자원 종류")]
    [SerializeField] private ProductionType type;
    [Header("건물 이름")]
    [SerializeField] private string facilityName;
    [Header("건물 설명")]
    [SerializeField] private string facilityInfo;
    [Header("초기 자원 생산량")]
    [SerializeField] private int defaultAmount;
    [Header("생산 건물 초기 내구도")]
    [SerializeField] private int defaulthp;
    [Header("생산 건물 초기 최대 주민 배치 수")]
    [SerializeField] private int defaultMaxWorker;
    [Header("생산 건물 건설에 필요한 자원")]
    [SerializeField] private List<ResourceCost> constructCost;
    [Header("생산 건물 업그레이드에 필요한 초기 자원")]
    [SerializeField] private List<ResourceCost> upgradeCost;

    public ProductionType Type => type;
    public int DefaultAmount => defaultAmount;
    public int DefaultHp => defaulthp;
    public int DefaultMaxWorker => defaultMaxWorker;
    public string FacilityName => facilityName;
    public string FacilityInfo => facilityInfo;
    public (ProductionType Type, int Amount)[] ConstructProduct
    {
        get
        {
            var temp = new (ProductionType, int)[constructCost.Count];

            for (int i = 0; i < constructCost.Count; i++)
            {
                temp[i] = (constructCost[i].Type, -constructCost[i].Amount);
            }

            return temp;
        }
    }

    public (ProductionType Type, int Amount)[] UpgradeCost
    {
        get
        {
            var temp = new (ProductionType, int)[upgradeCost.Count];

            for (int i = 0; i < upgradeCost.Count; i++)
            {
                temp[i] = (upgradeCost[i].Type, -upgradeCost[i].Amount);
            }

            return temp;
        }
    }
}
