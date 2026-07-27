using UnityEngine;

/// <summary>
/// 맵 메이커 창이 쓰는 색을 정한다.
///
/// 값은 어두운 에디터 스킨(#383838) 위에서 서로 구분되도록 고른 것이다 —
/// EditorGUI.DrawRect는 여기 적은 값을 그대로 화면에 쓰므로(감마 보정이 끼지 않는다)
/// 배경이나 다른 색과 가까운 값을 넣으면 그 자리에서 바로 구분이 안 된다.
///
/// 지형(큰 분류)은 바탕색으로, 배치 허용(작은 분류)은 칸 안의 점으로 나눈다.
/// </summary>
public static class MapMakerPalette
{
    /// <summary>어두운 에디터 스킨 배경. 고르지 않은 칸을 이 쪽으로 섞어 죽인다.</summary>
    public static readonly Color Panel = new(0.220f, 0.220f, 0.220f);

    public static readonly Color Ground = new(0.290f, 0.478f, 0.322f);
    public static readonly Color High = new(0.561f, 0.259f, 0.196f);
    public static readonly Color Border = new(0.353f, 0.290f, 0.431f);
    public static readonly Color Core = new(0.184f, 0.373f, 0.620f);
    public static readonly Color Nothing = new(0.420f, 0.420f, 0.420f);

    /// <summary>고지 칸 위쪽에 얹는 밝은 띠 — 한 단 올라와 있다는 표시.</summary>
    public static readonly Color HighTop = new(0.851f, 0.545f, 0.416f);

    /// <summary>본진 칸 가운데 표식.</summary>
    public static readonly Color CoreMark = new(0.737f, 0.851f, 1.000f);

    /// <summary>적 스폰 칸 테두리.</summary>
    public static readonly Color Spawn = new(0.910f, 0.753f, 0.290f);

    /// <summary>지금 고른 배치 허용이 켜져 있는 칸의 점.</summary>
    public static readonly Color Mark = new(0.918f, 0.918f, 0.918f);

    public static readonly Color Path = new(1.000f, 1.000f, 1.000f);
    public static readonly Color PathEdge = new(0.000f, 0.000f, 0.000f, 0.55f);
    public static readonly Color Problem = new(0.910f, 0.259f, 0.369f);

    /// <summary>프리팹과 다른(씬 오버라이드) 칸 표식 — 청록. 문제(빨강)·스폰(금)과 겹치지 않게 골랐다.</summary>
    public static readonly Color Override = new(0.216f, 0.804f, 0.831f);

    /// <summary>배치 허용이 켜졌지만 효과 없는(무효 조합) 칸 표식 — 주황.</summary>
    public static readonly Color Inert = new(0.960f, 0.510f, 0.129f);

    /// <summary>지형별 바탕색.</summary>
    public static Color Terrain(TerrainType terrain)
    {
        switch (terrain)
        {
            case TerrainType.Ground: return Ground;
            case TerrainType.High: return High;
            case TerrainType.Core: return Core;
            case TerrainType.Special: return Border;
            default: return Nothing;
        }
    }

    /// <summary>지금 고른 붓과 상관없는 칸을 배경 쪽으로 죽인다 — 무슨 모드인지 한눈에 보이게 하는 장치다.</summary>
    public static Color Dim(Color color)
    {
        return Color.Lerp(color, Panel, 0.68f);
    }
}
