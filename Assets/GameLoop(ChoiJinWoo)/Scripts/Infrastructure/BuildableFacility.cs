using System;
using UnityEngine;

[Serializable]
public class BuildableFacility
{
    public string label = "건물";
    public Sprite icon;
    public OccupantKind kind = OccupantKind.Resource;
    [Tooltip("kind == Resource일 때 채움.")]
    public ProductionValue facilityValue;
    [Tooltip("kind == Building일 때 채움.")]
    public HouseConfig houseConfig;

    public string DisplayName => kind == OccupantKind.Resource
        ? facilityValue != null ? facilityValue.FacilityDisplayName : label
        : houseConfig != null ? houseConfig.HouseDisplayName : label;
}
