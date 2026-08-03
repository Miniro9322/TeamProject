using System;
using UnityEngine;

// 맵에 배치할 영웅 슬롯 하나의 정보(라벨·프리팹·종류·사거리). 팔레트 리스트에 담겨 인스펙터에서 편집됨.
// 생산 시설·집은 더 이상 맵에 배치되지 않아 여기 안 들어간다 - BuildableFacility(기반시설 UI 전용) 참고.
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
