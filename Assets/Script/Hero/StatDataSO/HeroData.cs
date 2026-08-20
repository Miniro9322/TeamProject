using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "HeroData", menuName = "HeroData/HeroData")]
public class HeroData : ScriptableObject
{
    public struct AttackDescription
    {
        public Image icon;
        public string attackDescriptionKey;
    }
    public int UnitId;
    public int Tier;
    public string HeroName;
    public string HeroNameKey;
    public string HeroDescriptionKey;
    public int HeroType;
    public Sprite Icon;
    public GameObject HeroPrefab;
    public MergeKey MergeKey => new MergeKey(UnitId, Tier);
    public int PopulationCost => Tier;

    public List<AttackDescription> AttackDescriptions;
}