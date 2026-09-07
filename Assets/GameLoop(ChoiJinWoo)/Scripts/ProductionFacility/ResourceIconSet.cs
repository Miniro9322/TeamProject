using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ResourceIconEntry
{
    public ProductionType type;
    public Sprite icon;
}

[CreateAssetMenu(fileName = "ResourceIconSet", menuName = "Scriptable Objects/ResourceIconSet")]
public class ResourceIconSet : ScriptableObject
{
    [SerializeField] private List<ResourceIconEntry> icons;

    public Sprite GetIcon(ProductionType type)
    {
        foreach (var entry in icons)
        {
            if (entry.type == type) return entry.icon;
        }
        return null;
    }
}
