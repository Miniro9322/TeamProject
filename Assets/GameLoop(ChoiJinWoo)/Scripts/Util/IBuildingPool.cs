using UnityEngine;

public interface IBuildingPool
{
    GameObject Rent(ProductionType type);
    void Return(GameObject instance);
}