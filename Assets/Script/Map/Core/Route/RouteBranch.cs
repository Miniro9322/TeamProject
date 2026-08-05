using System.Collections.Generic;
using UnityEngine;

// 스폰 타일에서 갈라져 나가는 길 한 줄. 저작 경유점과 구워진 발자국을 함께 보관합니다.
[RequireComponent(typeof(TileRoute))]
public sealed class RouteBranch : MonoBehaviour
{
    [SerializeField] private List<RouteNode> nodes = new();

    private List<Tile> trail = new();
    private List<Vector3> points = new();

    // 이 갈래가 출발하는 스폰 타일.
    public Tile Spawn { get; private set; }

    // 사람이 찍은 경유점. 길을 다시 고칠 때 쓰는 원본입니다.
    public IReadOnlyList<RouteNode> Nodes => nodes;

    // 빈틈 없이 펼쳐진 발자국 칸.
    public IReadOnlyList<Tile> Trail => trail;

    // 발자국의 월드 좌표. 적이 그대로 읽고 움직입니다.
    public IReadOnlyList<Vector3> Points => points;

    // 발자국이 구워져 있는가.
    public bool IsBaked => trail.Count > 0;

    // 자기 스폰 타일을 잡고 그 타일의 갈래 목록에 자신을 등록합니다.
    private void Awake()
    {
        Spawn = GetComponent<Tile>();
        GetComponent<TileRoute>().Add(this);
    }

    // 베이크가 끝난 발자국 줄과 월드 좌표를 넘겨받아 보관합니다.
    public void Keep(List<Tile> bakedTrail, List<Vector3> bakedPoints)
    {
        trail = bakedTrail;
        points = bakedPoints;
    }
}
