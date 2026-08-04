using System;
using UnityEngine;

// 경로가 거쳐 가는 칸 하나. 어디를 지나는지와 거기서 몇 초 멈추는지를 함께 들고 있습니다.
[Serializable]
public struct RouteNode
{
    [SerializeField] private Vector2Int coord;
    [SerializeField] private float waitTime;

    public Vector2Int Coord => coord;
    public float WaitTime => waitTime;
}
