using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 한 칸의 타일 실물을 지형 프리팹으로 갈아끼우는 에디터 전용 도구.
///
/// 지형 붓이 State.Terrain만 바꾸면 데이터는 High인데 큐브는 여전히 납작한 지상이다 —
/// 데이터와 겉모습이 갈라진다. 여기서 실물을 바꿔 둘을 맞춘다.
///
/// 한 칸은 오브젝트 하나가 아니다. 고지 칸은 밑판 + 그 위에 얹은 얇은 판 두 겹이고,
/// 외곽은 밑판 + 벽 두 겹이다. 그래서 "바꾸기"는 교체(밑판)와 쌓기·걷어내기(윗판)로 나뉜다.
///
/// 크기는 프리팹 저작값을 믿지 않고 그 모듈 격자에 맞춘다 — 셀 크기가 2인 맵에
/// 1짜리 큐브를 찍으면 반쪽이 박히기 때문이다. 고지 판의 두께는 한 단(셀의 절반)으로 맞춘다.
/// </summary>
public static class TileSwap
{
    /// <summary>고지 판 두께가 셀 크기에서 차지하는 비율(한 단 = 반 칸). 기존 저작값과 같다.</summary>
    private const float StepRatio = 0.5f;

    /// <summary>이 칸에 쌓인 타일 전부를 아래에서 위 순으로.</summary>
    public static List<Tile> Stack(Grid module, Vector2Int coord)
    {
        var layers = new List<Tile>();
        foreach (Tile tile in module.GetComponentsInChildren<Tile>(true))
        {
            if (tile.Coord == coord)
            {
                layers.Add(tile);
            }
        }

        layers.Sort((a, b) => a.transform.position.y.CompareTo(b.transform.position.y));
        return layers;
    }

    /// <summary>
    /// 이 칸을 해당 지형으로 만든다. 새 대표 타일(제일 위)을 돌려준다.
    ///
    /// 고지면 밑판 위에 판을 얹고, 아니면 얹혀 있던 고지 판을 걷어낸 뒤 밑판을 갈아끼운다.
    /// 벽(고지가 아닌 윗겹)은 건드리지 않는다 — 지형을 바꾸려다 외곽이 사라지지 않게 한다.
    /// </summary>
    public static Tile Apply(Grid module, Vector2Int coord, GameObject prefab, TerrainType terrain)
    {
        List<Tile> layers = Stack(module, coord);
        if (layers.Count == 0)
        {
            return null;
        }

        float cell = module.cellSize.x;

        if (terrain == TerrainType.High)
        {
            Tile top = layers[layers.Count - 1];
            if (top.State.Terrain == TerrainType.High)
            {
                return top; // 이미 고지 — 판을 겹쳐 쌓지 않는다
            }

            return Raise(top, prefab, cell);
        }

        Tile bottom = layers[0];
        foreach (Tile layer in layers)
        {
            if (layer != bottom && layer.State.Terrain == TerrainType.High)
            {
                Undo.DestroyObjectImmediate(layer.gameObject); // 얹혀 있던 고지 판을 걷어낸다
            }
        }

        return Replace(bottom, prefab, terrain, cell);
    }

    // 밑판을 같은 자리에 다른 프리팹으로 교체한다. 좌표·스폰 표식은 옮기고, 배치 허용은 새 지형 기본값으로 둔다.
    private static Tile Replace(Tile old, GameObject prefab, TerrainType terrain, float cell)
    {
        Transform from = old.transform;
        Tile made = Create(prefab, from.parent, from.position, from.rotation, cell);

        made.transform.SetSiblingIndex(from.GetSiblingIndex());
        made.State.Terrain = terrain;
        made.State.Col = old.State.Col;
        made.State.Row = old.State.Row;
        made.isEnemySpawn = old.isEnemySpawn;
        EditorUtility.SetDirty(made);

        Undo.DestroyObjectImmediate(old.gameObject);
        return made;
    }

    // 밑판 윗면에 고지 판을 얹는다. 밑판은 그대로 두어 옆면이 뚫리지 않게 한다.
    private static Tile Raise(Tile support, GameObject prefab, float cell)
    {
        Transform under = support.transform;
        var spot = new Vector3(under.position.x, TopY(support), under.position.z);

        Tile made = Create(prefab, under.parent, spot, under.rotation, cell);
        Scale(made.transform, cell * StepRatio, true); // 한 단 두께로 눌러 놓는다

        made.State.Terrain = TerrainType.High;
        made.State.Col = support.State.Col;
        made.State.Row = support.State.Row;
        EditorUtility.SetDirty(made);
        return made;
    }

    // 프리팹 링크를 살려 생성한다 — 프리팹 모드면 원본에, 씬이면 그 씬에만 남는다(창의 ◆프리팹/○씬 표시가 그 경고다).
    private static Tile Create(GameObject prefab, Transform parent, Vector3 spot, Quaternion turn, float cell)
    {
        var made = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        Undo.RegisterCreatedObjectUndo(made, "Swap Tile");

        Transform tr = made.transform;
        tr.SetPositionAndRotation(spot, turn);
        Scale(tr, cell, false); // 격자 칸을 꽉 채우게 맞춘다

        return made.GetComponent<Tile>();
    }

    // 렌더러 실측을 재서 원하는 치수가 되도록 스케일을 곱한다. height=true면 높이를, 아니면 가로를 맞춘다.
    private static void Scale(Transform tr, float target, bool height)
    {
        float now = height ? SizeY(tr) : SizeX(tr);
        if (now <= 0.0001f)
        {
            return; // 잴 면이 없다 — 저작값 그대로 둔다
        }

        float factor = target / now;
        Vector3 scale = tr.localScale;

        if (height)
        {
            scale.y *= factor;
        }
        else
        {
            scale *= factor;
        }

        tr.localScale = scale;
    }

    private static float SizeX(Transform tr)
    {
        foreach (Renderer rend in tr.GetComponentsInChildren<Renderer>())
        {
            return rend.bounds.size.x;
        }

        return 0f;
    }

    private static float SizeY(Transform tr)
    {
        foreach (Renderer rend in tr.GetComponentsInChildren<Renderer>())
        {
            return rend.bounds.size.y;
        }

        return 0f;
    }

    /// <summary>이 타일의 윗면 높이 — 위에 판을 얹을 자리.</summary>
    private static float TopY(Tile tile)
    {
        float top = tile.transform.position.y;
        bool measured = false;

        foreach (Renderer rend in tile.GetComponentsInChildren<Renderer>())
        {
            if (!measured || rend.bounds.max.y > top)
            {
                top = rend.bounds.max.y;
                measured = true;
            }
        }

        return top;
    }
}
