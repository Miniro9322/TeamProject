using UnityEngine;

[CreateAssetMenu(fileName = "BaseUpgradeData", menuName = "Scriptable Objects/BaseUpgradeData")]
public class BaseUpgradeData : ScriptableObject
{
    public string id;
    public string displayName;
    public Sprite icon;
    public Color iconColor = Color.white;
    public int cost;
    public string description;
    public BaseUpgradeData[] prerequisites;
    public float effectAmount;
    public bool isLast;
}
