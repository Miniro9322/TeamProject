using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

public class ResourceTest : MonoBehaviour
{
    [SerializeField] private List<ProductionFacility> facilityPrefabs;
    private List<ProductionFacility> obj = new();
    private ResourcesManager manager;
    private IObjectResolver resolver;

    [Inject]
    public void Construct(IObjectResolver resolver)
    {
        this.resolver = resolver;
    }

    [Inject]
    public void Construct(ResourcesManager resourcesManager)
    {
        manager = resourcesManager;
    }

    private void Start()
    {
        manager.ProductUpdate += ResourcesUpdateTest;
    }

    private void OnDestroy()
    {
        manager.ProductUpdate -= ResourcesUpdateTest;
    }

    private void Update()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            Debug.Log("pressed");
            obj.Add(resolver.Instantiate(facilityPrefabs[0]));
        }

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            Debug.Log("pressed");
            obj.Add(resolver.Instantiate(facilityPrefabs[1]));
        }

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            Debug.Log("pressed");
            obj.Add(resolver.Instantiate(facilityPrefabs[2]));
        }

        if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            Debug.Log("pressed");
            obj.Add(resolver.Instantiate(facilityPrefabs[3]));
        }

        if (Keyboard.current.digit5Key.wasPressedThisFrame)
        {
            Debug.Log("pressed");
            obj.Add(resolver.Instantiate(facilityPrefabs[4]));
        }

        if (Keyboard.current.aKey.wasPressedThisFrame)
        {
            Debug.Log("pressed");
            if (obj != null)
            {
                foreach(var item in obj)
                {
                    item.ProduceProduction();
                }
            }
        }
    }

    private void ResourcesUpdateTest(ProductionType type, int amount)
    {
        Debug.Log($"{type}이 {amount}만큼 증가");
    }
}
