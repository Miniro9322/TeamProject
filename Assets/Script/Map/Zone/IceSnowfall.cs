using UnityEngine;

// 얼음 보드 전체를 덮는 상시 눈발 효과. 조건 판정 없이 시작 시 한 번만 만든다.
public class IceSnowfall
{
    // 보드 전체를 덮도록 크기를 맞춰 얼음 보드 자식으로 만든다.
    public IceSnowfall(MapBoard board, GameObject prefab)
    {
        Bounds bounds = board.WorldBounds;
        GameObject instance = Object.Instantiate(prefab, bounds.center, Quaternion.identity, board.transform);
        ParticleSystem.ShapeModule shape = instance.GetComponent<ParticleSystem>().shape;
        shape.scale = new Vector3(bounds.size.x, 1f, bounds.size.z);
    }
}
