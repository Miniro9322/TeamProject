using System.Collections.Generic;
using UnityEngine;

// 이 모듈에 지정된 경로 목록을 보관하고 스폰 좌표로 조회합니다. 한 스폰에 여러 벌을 담습니다.
[DisallowMultipleComponent]
public sealed class RouteConfig : MonoBehaviour
{
    [SerializeField] private List<RouteData> routes = new();

    private readonly Dictionary<Vector2Int, List<RouteData>> routeMap = new();

    private void Awake()
    {
        Rebuild();
    }

    private void OnValidate()
    {
        Rebuild();
    }

    // 이 스폰에 지정된 경로들을 찾습니다. 없으면 false.
    public bool TryGetRoutes(Vector2Int spawn, out List<RouteData> found)
    {
        return routeMap.TryGetValue(spawn, out found);
    }

    // 이 경로가 목록 몇 번째인지. 맵 메이커가 고른 경로를 고칠 때 쓰는 통로다.
    public int IndexOf(RouteData route)
    {
        return routes.IndexOf(route);
    }

    // 이 번호의 경로. 방금 만든 항목을 되받을 때 쓴다.
    public RouteData RouteAt(int index)
    {
        return routes[index];
    }

    // 지정 목록을 스폰 좌표 사전으로 정리합니다. 맵 메이커가 목록을 고친 뒤에도 부릅니다.
    public void Rebuild()
    {
        routeMap.Clear();

        for (int index = 0; index < routes.Count; index++)
        {
            AddToMap(routes[index]);
        }
    }

    private void AddToMap(RouteData route)
    {
        if (routeMap.TryGetValue(route.Spawn, out List<RouteData> found))
        {
            found.Add(route);
            return;
        }

        routeMap[route.Spawn] = new List<RouteData> { route };
    }
}
