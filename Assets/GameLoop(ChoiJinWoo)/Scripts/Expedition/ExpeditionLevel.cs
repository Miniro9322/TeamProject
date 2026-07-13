using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ExpeditionLevel", menuName = "Scriptable Objects/ExpeditionLevel")]
public class ExpeditionLevel : ScriptableObject
{
    [SerializeField] private int recommendStat;

    [Serializable]
    public class Rewards
    {
        [SerializeField] private ProductionType productionType;
        [SerializeField] private int amount;
    }

    [SerializeField] private List<Rewards> reward;

    public int RecommendStat => recommendStat;
    public List<Rewards> Reward => reward;
}
