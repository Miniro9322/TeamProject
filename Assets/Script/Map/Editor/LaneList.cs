using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 스폰마다 만들어진 경로를 한 줄씩 적는다.
///
/// 격자에서는 경로가 전부 같은 흰 선으로 겹쳐 그려져서, 둘 이상이면 어느 스폰에서 나온 길인지 가려지고
/// 막힌 스폰은 아무것도 그려지지 않아 화면에서 사라진다. 여기서 스폰별로 갈라 적어 그 둘을 드러낸다.
///
/// 줄을 누르면 그 스폰 타일을 계층에서 고른다 — 막힌 스폰을 눈으로 찾아다니지 않게 한다.
/// </summary>
public static class LaneList
{
    /// <summary>경로 줄들. 고른 경로의 번호를 돌려준다(누르지 않았으면 받은 값 그대로).</summary>
    public static int Draw(IReadOnlyList<LaneData> lanes, int chosen)
    {
        if (lanes.Count == 0)
        {
            GUILayout.Label("적 스폰이 없습니다 — 스폰 붓으로 시작 칸을 찍으면 경로가 여기 나옵니다.",
                EditorStyles.wordWrappedMiniLabel);
            return chosen;
        }

        int picked = chosen;
        for (int i = 0; i < lanes.Count; i++)
        {
            if (DrawLane(lanes[i], i == chosen))
            {
                picked = i;
            }
        }

        return picked;
    }

    private static bool DrawLane(LaneData lane, bool chosen)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            Rect mark = GUILayoutUtility.GetRect(11f, 13f, GUILayout.Width(11));
            EditorGUI.DrawRect(new Rect(mark.x, mark.y + 2f, 9f, 9f),
                lane.IsValid ? MapMakerPalette.Path : MapMakerPalette.Problem);

            GUILayout.Label(Word(lane), chosen ? EditorStyles.miniBoldLabel : EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();

            if (!GUILayout.Button("스폰 보기", EditorStyles.miniButton, GUILayout.Width(58)))
            {
                return false;
            }

            if (lane.Start != null)
            {
                Selection.activeGameObject = lane.Start.gameObject;
                EditorGUIUtility.PingObject(lane.Start.gameObject);
            }

            return true;
        }
    }

    private static string Word(LaneData lane)
    {
        string from = lane.Start != null ? lane.Start.Coord.ToString() : "(스폰 없음)";
        if (!lane.IsValid)
        {
            return $"스폰 {from} · 막힘 — 본진까지 가는 길이 없습니다";
        }

        return $"스폰 {from} · {lane.Tiles.Count}칸 · 정상";
    }
}
