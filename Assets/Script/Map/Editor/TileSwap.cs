using UnityEditor;
using UnityEngine;

/// <summary>
/// 한 칸의 타일 GameObject를 지형 프리팹으로 통째 갈아끼우는 에디터 전용 도구.
///
/// 지형 붓이 State.Terrain만 바꾸면 데이터는 High인데 큐브 메시는 여전히 납작한 Ground다 —
/// 데이터와 겉모습이 갈라진다. 스왑은 실물을 교체해 둘을 맞춘다.
///
/// 좌표(Col/Row)와 스폰 표식은 옛 타일에서 새 타일로 옮긴다(같은 자리라 재베이크가 필요 없다).
/// 배치 허용은 옮기지 않는다 — 지형이 바뀌면 대개 무효가 되므로 새 프리팹 기본값으로 둔다.
/// </summary>
public static class TileSwap
{
    /// <summary>
    /// old 타일을 prefab 인스턴스로 교체하고 새 Tile을 돌려준다.
    ///
    /// InstantiatePrefab으로 프리팹 링크를 살려 둔다 — 프리팹 모드에서 하면 원본에 반영되고,
    /// 씬에서 하면 그 씬에만 남는 구조 오버라이드가 된다(창의 ◆프리팹/○씬 라벨이 그 경고다).
    /// 생성과 삭제 모두 Undo에 등록해 붓질 묶음(BeginStroke) 한 번에 되돌아간다.
    /// </summary>
    public static Tile Swap(Tile old, GameObject prefab, TerrainType terrain)
    {
        Transform from = old.transform;

        var made = (GameObject)PrefabUtility.InstantiatePrefab(prefab, from.parent);
        Undo.RegisterCreatedObjectUndo(made, "Swap Tile");

        Transform to = made.transform;
        to.SetPositionAndRotation(from.position, from.rotation);
        to.localScale = from.localScale;
        to.SetSiblingIndex(from.GetSiblingIndex());

        var tile = made.GetComponent<Tile>();

        // 프리팹이 어떻게 저작됐든 데이터가 확실히 맞도록 지형을 직접 박는다.
        tile.State.Terrain = terrain;
        tile.State.Col = old.State.Col;
        tile.State.Row = old.State.Row;
        tile.isEnemySpawn = old.isEnemySpawn;
        EditorUtility.SetDirty(tile);

        Undo.DestroyObjectImmediate(old.gameObject);
        return tile;
    }
}
