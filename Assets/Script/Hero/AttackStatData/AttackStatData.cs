using UnityEngine;

[CreateAssetMenu(fileName = "AttackStatData", menuName = "AttackStatData/AttackStatData")]
public class AttackStatData : ScriptableObject
{
    public int chainCount = 1;
    public int chainArea = 1;
    public int attackArea = 1;
    public int attackCount = 1;
    public float drainRate = 0.1f;
    public bool isHeal = false;
    public EnemyAttribute unattackable = EnemyAttribute.Fly | EnemyAttribute.Cloaking;
}


