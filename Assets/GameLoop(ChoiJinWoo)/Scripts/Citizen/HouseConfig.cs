using System.Collections.Generic;
using UnityEngine;

// House가 MonoBehaviour였을 때 프리팹 인스펙터에 있던 데이터를 그대로 옮긴 SO.
[CreateAssetMenu(fileName = "HouseConfig", menuName = "Scriptable Objects/HouseConfig")]
public class HouseConfig : ScriptableObject
{
    [SerializeField] private int maxCitizenAmount;
    [Header("건설에 필요한 자원")]
    [SerializeField] private List<ResourceCost> cost;
    [Header("건물 이름")]
    [SerializeField] private string houseName;
    [Header("건물 설명")]
    [SerializeField]
    [TextArea] private string houseInfo;
    [Header("강화 1회당 추가되는 최대 시민 수")]
    [SerializeField] private int citizenPerUpgrade;
    [Header("강화에 필요한 초기 자원")]
    [SerializeField] private List<ResourceCost> upgradeCost;

    public int MaxCitizenAmount => maxCitizenAmount;
    public string HouseName => houseName;
    public string HouseInfo => houseInfo;
    public int CitizenPerUpgrade => citizenPerUpgrade;

    public (ProductionType Type, int Amount)[] Resources => cost.ToNegatedCostArray();
    public (ProductionType Type, int Amount)[] UpgradeCost => upgradeCost.ToNegatedCostArray();
}
