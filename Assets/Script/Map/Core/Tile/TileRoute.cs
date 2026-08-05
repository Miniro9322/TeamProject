using System.Collections.Generic;
using UnityEngine;

// 스폰 타일이 자기에게서 갈라지는 길 갈래들을 보관합니다.
[DisallowMultipleComponent]
[RequireComponent(typeof(Tile))]
public sealed class TileRoute : MonoBehaviour
{
    private readonly List<RouteBranch> branches = new();

    // 이 스폰에서 뻗어 나가는 갈래 목록. 갈래가 스스로 등록합니다.
    public IReadOnlyList<RouteBranch> Branches => branches;

    // 등록된 갈래 수.
    public int BranchCount => branches.Count;

    // 갈래 하나를 목록 맨 뒤에 등록합니다.
    public void Add(RouteBranch branch)
    {
        branches.Add(branch);
    }
}
