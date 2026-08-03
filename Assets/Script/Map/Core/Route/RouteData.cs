using System;
using System.Collections.Generic;
using UnityEngine;

// 제작자가 지정한 경로 한 벌을 보관합니다.
[Serializable]
public sealed class RouteData
{
    [SerializeField] private Vector2Int spawn;
    [SerializeField] private List<RouteNode> nodes = new();

    public Vector2Int Spawn => spawn;
    public IReadOnlyList<RouteNode> Nodes => nodes;
}
