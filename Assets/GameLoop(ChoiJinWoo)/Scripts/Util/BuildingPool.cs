using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class BuildingPool : IBuildingPool
{
    private readonly IObjectResolver _resolver;
    private readonly IReadOnlyDictionary<ProductionType, GameObject> _prefabs;
    private readonly Dictionary<ProductionType, Stack<GameObject>> _pools = new();
    private readonly ResourcesManager resourcesManager;

    public BuildingPool(IObjectResolver resolver, BuildingPrefabRegistry registry, ResourcesManager resourcesManager)
    {
        _resolver = resolver;
        _prefabs = registry.Prefabs;
        this.resourcesManager = resourcesManager;
    }

    public GameObject Rent(ProductionType type)
    {
        if (!_prefabs.TryGetValue(type, out var prefab))
        {
            Debug.LogError($"등록되지 않은 타입: {type}");
            return null;
        }

        if (!resourcesManager.CheckResources(prefab.GetComponent<ProductionFacility>().BasicValue.ConstructProduct))
        {
            Debug.LogWarning("자원이 부족합니다.");
            return null;
        }

        GameObject instance;
        if (_pools.TryGetValue(type, out var stack) && stack.Count > 0)
        {
            instance = stack.Pop();
            instance.gameObject.SetActive(true);
        }
        else
        {
            instance = _resolver.Instantiate(prefab);
        }

        instance.GetComponent<ProductionFacility>().Init();
        return instance;
    }

    public void Return(GameObject instance)
    {
        instance.SetActive(false);

        var type = instance.GetComponent<ProductionFacility>().BasicValue.Type;
        if (!_pools.TryGetValue(type, out var stack))
        {
            stack = new Stack<GameObject>();
            _pools[type] = stack;
        }
        stack.Push(instance);
    }

    public void TryRent(Tile tile)
    {
        if (tile == null || tile.OccupantObject == null) return;

        var facility = tile.OccupantObject.GetComponent<ProductionFacility>();
        if (facility == null) return;

        _resolver.InjectGameObject(tile.OccupantObject);
        facility.Init();
    }
}