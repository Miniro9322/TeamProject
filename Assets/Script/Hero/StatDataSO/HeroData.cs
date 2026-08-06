using UnityEngine;

[CreateAssetMenu(fileName = "HeroData", menuName = "HeroData/HeroData")]
public class HeroData : ScriptableObject
{
    public int UnitId;
    public int Tier;
    public string HeroName;
}
