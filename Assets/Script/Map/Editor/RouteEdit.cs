using UnityEditor;
using UnityEngine;

/// <summary>
/// 맵 메이커의 경로 도구가 RouteConfig의 저작 목록을 고치는 에디터 전용 도구.
///
/// SerializedObject로만 쓴다 — RouteConfig·RouteData의 필드는 private이고, 이쪽으로 고치면
/// 되돌리기와 프리팹 오버라이드 처리를 유니티가 맡는다(런타임 코드에 저작용 API를 만들지 않는다).
///
/// 저장 자리는 MapBoard가 붙은 오브젝트다. 모듈은 뿌리(MapModule_A)에 MapBoard·EnemyLanes를 두고
/// Grid는 그 자식이라, Grid에 붙이면 경로만 EnemyLanes와 다른 오브젝트로 갈라진다.
/// </summary>
public static class RouteEdit
{
    private const string RoutesField = "routes";
    private const string SpawnField = "spawn";
    private const string NodesField = "nodes";

    /// <summary>이 모듈의 RouteConfig. 아직 없으면 null.</summary>
    public static RouteConfig Find(Grid module)
    {
        MapBoard board = Owner(module);
        if (board == null)
        {
            return null;
        }

        return board.GetComponent<RouteConfig>();
    }

    /// <summary>저작할 RouteConfig를 확보한다. 없으면 MapBoard 옆에 새로 붙인다.</summary>
    public static RouteConfig Ensure(Grid module)
    {
        MapBoard board = Owner(module);
        if (board == null)
        {
            return null;
        }

        RouteConfig found = board.GetComponent<RouteConfig>();
        if (found != null)
        {
            return found;
        }

        Warn(board);
        return Undo.AddComponent<RouteConfig>(board.gameObject);
    }

    /// <summary>
    /// 조회 사전을 목록과 맞춘다. RouteConfig는 Awake·OnValidate에서만 사전을 다시 세우는데,
    /// 되돌리기는 OnValidate를 부른다는 보장이 없어 창이 지워진 경로를 계속 그리게 된다.
    /// </summary>
    public static void Sync(RouteConfig config)
    {
        if (config == null)
        {
            return;
        }

        config.Rebuild();
    }

    /// <summary>이 좌표를 경유 노드로 넣는다. 이미 들어 있으면 뺀다.</summary>
    public static void ToggleNode(RouteConfig config, Vector2Int spawn, Vector2Int node)
    {
        var owner = new SerializedObject(config);
        SerializedProperty route = GetRoute(owner, spawn);
        SerializedProperty nodes = route.FindPropertyRelative(NodesField);

        int index = IndexOf(nodes, node);
        if (index < 0)
        {
            Add(nodes, node);
        }
        else
        {
            nodes.DeleteArrayElementAtIndex(index);
        }

        owner.ApplyModifiedProperties();
    }

    // 경로가 붙어 사는 오브젝트. 비활성 모듈에서도 찾아야 한다(잠긴 모듈도 저작 대상이다).
    private static MapBoard Owner(Grid module)
    {
        return module.GetComponentInParent<MapBoard>(true);
    }

    // 씬 인스턴스에 컴포넌트를 새로 붙이면 그 씬에만 남는다. 값 오버라이드보다 눈에 안 띄어 미리 알린다.
    private static void Warn(MapBoard board)
    {
        if (ModuleScan.IsPrefabStage())
        {
            return;
        }

        Debug.LogWarning("[Map Maker] 씬 인스턴스에 RouteConfig를 새로 붙입니다 — " +
            "저작한 경로는 이 씬에만 남습니다. 모듈 프리팹을 열고 찍으면 모든 씬에 반영됩니다.", board);
    }

    // 이 스폰의 경로 항목. 없으면 목록 끝에 새로 만든다.
    private static SerializedProperty GetRoute(SerializedObject owner, Vector2Int spawn)
    {
        SerializedProperty routes = owner.FindProperty(RoutesField);

        for (int i = 0; i < routes.arraySize; i++)
        {
            SerializedProperty route = routes.GetArrayElementAtIndex(i);
            if (route.FindPropertyRelative(SpawnField).vector2IntValue == spawn)
            {
                return route;
            }
        }

        routes.arraySize++;
        SerializedProperty added = routes.GetArrayElementAtIndex(routes.arraySize - 1);
        added.FindPropertyRelative(SpawnField).vector2IntValue = spawn;
        added.FindPropertyRelative(NodesField).ClearArray();
        return added;
    }

    // 목록에서 같은 좌표의 자리. 없으면 -1.
    private static int IndexOf(SerializedProperty nodes, Vector2Int node)
    {
        for (int i = 0; i < nodes.arraySize; i++)
        {
            if (nodes.GetArrayElementAtIndex(i).vector2IntValue == node)
            {
                return i;
            }
        }

        return -1;
    }

    // 목록 끝에 좌표를 붙인다 — 찍은 차례가 곧 지나갈 차례다.
    private static void Add(SerializedProperty nodes, Vector2Int node)
    {
        nodes.arraySize++;
        nodes.GetArrayElementAtIndex(nodes.arraySize - 1).vector2IntValue = node;
    }
}
