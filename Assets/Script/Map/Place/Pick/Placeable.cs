using System;
using UnityEngine;

// 배치할 슬롯 하나의 정보(라벨·프리팹·종류·사거리). 팔레트 리스트에 담겨 인스펙터에서 편집됨.
// kind는 살아있는 값 — 배치 규칙(TilePlacementRule)·풀 대여(UnitPlacer)가 사용한다.
[Serializable]
public class Placeable
{
    public string label = "유닛";
    public GameObject prefab;
    public Sprite icon;
    public Sprite placedIcon;
    public OccupantKind kind = OccupantKind.MeleeHero;
    [Min(0)] public int attackRange;
}
