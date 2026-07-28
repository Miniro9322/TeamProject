using UnityEngine;

/// <summary>
/// 스왑 브러시가 쓰는 '지형 → 타일 프리팹' 매핑. 에디터 저작 전용 설정이다.
///
/// 경로가 아니라 에셋 참조로 잡는다 — 프리팹 폴더가 바뀌어도(Tile_Test로 옮긴 것처럼) 안 깨지고,
/// Forest 같은 변형으로 슬롯만 갈아끼우면 그 지형 붓이 곧 다른 프리팹을 찍는다.
/// 키는 TerrainType이다(MapBrush가 아니라) — 붓 종류가 아니라 지형이 프리팹을 정하기 때문이다.
/// </summary>
[CreateAssetMenu(fileName = "TilePrefabSet", menuName = "Map/Tile Prefab Set")]
public class TilePrefabSet : ScriptableObject
{
    public GameObject Ground;
    public GameObject High;
    public GameObject Core;
    public GameObject Special;

    /// <summary>이 지형을 찍을 실물 프리팹. 슬롯이 비어 있으면 null(스왑 안 함, 데이터 칠로 넘어간다).</summary>
    public GameObject For(TerrainType terrain)
    {
        switch (terrain)
        {
            case TerrainType.Ground: return Ground;
            case TerrainType.High: return High;
            case TerrainType.Core: return Core;
            case TerrainType.Special: return Special;
            default: return null;
        }
    }
}
