using UnityEngine;

// 배치물이 몇 칸을 차지하는지 낸다. 크기의 원본은 SO 하나뿐이라 슬롯·씬마다 갈리지 않는다.
public static class PlaceSize
{
    public static Vector2Int GetSize(Placeable slot)
    {
        if (slot.prefab == null)
        {
            return Vector2Int.one;   // 프리팹 없는 슬롯은 저작 실수 — UnitPlacer.CheckPrefab이 배치 시점에 알린다
        }

        if (slot.prefab.TryGetComponent(out ProductionFacility facility) && facility.BasicValue != null)
        {
            return facility.BasicValue.TileSize;
        }

        return Vector2Int.one;
    }
}
