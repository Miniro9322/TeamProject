using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildingPrefabRegistry", menuName = "Config/Building Prefab Registry")]
public class BuildingPrefabRegistry : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public ProductionType type;
        public GameObject prefab;
    }

    [SerializeField] private List<Entry> entries;

    private Dictionary<ProductionType, GameObject> _map;

    public Dictionary<ProductionType, GameObject> Prefabs
    {
        get
        {
            if (_map == null)
            {
                _map = new Dictionary<ProductionType, GameObject>();
                foreach (var e in entries)
                    _map[e.type] = e.prefab;
            }
            return _map;
        }
    }
}