using System.Collections.Generic;
using UnityEngine;

// 이 모듈에 저작된 경로 목록을 보관하고 스폰 좌표로 조회합니다.
[DisallowMultipleComponent]
public sealed class RouteConfig : MonoBehaviour
{
    [SerializeField] private List<RouteData> routes = new();

    private readonly Dictionary<Vector2Int, RouteData> routeMap = new();

    private void Awake()
    {
        Rebuild();
    }

    private void OnValidate()
    {
        Rebuild();
    }

    // 스폰 좌표에 저작된 경로를 찾습니다.
    public bool TryGetRoute(Vector2Int spawn, out RouteData route)
    {
        return routeMap.TryGetValue(spawn, out route);
    }

    // 저작 목록을 스폰 좌표 사전으로 정리합니다.
    private void Rebuild()
    {
        routeMap.Clear();

        for (int i = 0; i < routes.Count; i++)
        {
            routeMap[routes[i].Spawn] = routes[i];
        }
    }
}
