using UnityEditor;
using UnityEngine;

/// <summary>
/// 스왑 매핑 에셋(TilePrefabSet)을 찾고, 없으면 기본값으로 하나 만드는 에디터 전용 도구.
///
/// 기본값은 Tile_Test 폴더의 큐브 프리팹을 이름으로 찾아 채운다 — 폴더가 다시 움직여도
/// 이름만 같으면 붙는다. 채운 뒤엔 사용자가 인스펙터에서 슬롯을 자유로이 갈아끼운다.
/// </summary>
public static class TilePrefabSetIO
{
    private const string AssetPath = "Assets/Script/Map/Editor/TilePrefabSet.asset";

    /// <summary>프로젝트에 있는 첫 TilePrefabSet. 없으면 null.</summary>
    public static TilePrefabSet Load()
    {
        string[] guids = AssetDatabase.FindAssets("t:TilePrefabSet");
        if (guids.Length == 0)
        {
            return null;
        }

        return AssetDatabase.LoadAssetAtPath<TilePrefabSet>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    /// <summary>기본 매핑 에셋을 만들어 저장하고 돌려준다. 슬롯은 이름으로 찾은 큐브로 채운다.</summary>
    public static TilePrefabSet CreateDefault()
    {
        var set = ScriptableObject.CreateInstance<TilePrefabSet>();
        set.Ground = FindPrefab("Cube_Ground");
        set.High = FindPrefab("Cube_High");
        set.Core = FindPrefab("Cube_Core");
        set.Special = FindPrefab("Cube_Border");

        AssetDatabase.CreateAsset(set, AssetPath);
        AssetDatabase.SaveAssets();
        return set;
    }

    // 이름이 정확히 일치하는 프리팹만 — FindAssets는 부분 일치라 Cube_Ground가
    // Cube__GroundA_Production까지 물어 온다. 파일명으로 다시 걸러 정확한 하나만 집는다.
    private static GameObject FindPrefab(string exactName)
    {
        foreach (string guid in AssetDatabase.FindAssets($"{exactName} t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == exactName)
            {
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }

        return null;
    }
}
