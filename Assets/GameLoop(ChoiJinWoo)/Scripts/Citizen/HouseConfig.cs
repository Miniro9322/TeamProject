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

    public int MaxCitizenAmount => maxCitizenAmount;
    public string HouseName => houseName;
    public string HouseInfo => houseInfo;

    public (ProductionType Type, int Amount)[] Resources => cost.ToNegatedCostArray();
}
