using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 스폰마다 만들어진 경로를 한 줄씩 적는다.
///
/// 격자에서는 경로가 전부 같은 흰 선으로 겹쳐 그려져서, 둘 이상이면 어느 스폰에서 나온 길인지 가려지고
/// 막힌 스폰은 아무것도 그려지지 않아 화면에서 사라진다. 여기서 스폰별로 갈라 적어 그 둘을 드러낸다.
///
/// 줄을 누르면 그 경로가 편집 대상이 되고, 격자에서는 그 경로만 남고 나머지가 회색으로 죽는다.
/// </summary>
public static class LaneList
{
    /// <summary>경로 줄들. 고른 경로의 번호를 돌려준다(누르지 않았으면 받은 값 그대로).</summary>
    public static int Draw(IReadOnlyList<LaneData> lanes, int chosen, RouteConfig routes)
    {
        if (lanes.Count == 0)
        {
            GUILayout.Label("적 스폰이 없습니다 — 스폰 붓으로 시작 칸을 찍으면 경로가 여기 나옵니다.",
                EditorStyles.wordWrappedMiniLabel);
            return chosen;
        }

        int picked = chosen;

        // 하나를 고르면 나머지가 죽으므로, 전부 다시 보는 길을 같은 자리에 둔다.
        using (new EditorGUI.DisabledScope(chosen < 0))
        {
            if (GUILayout.Button("← 전체 보기 (모든 경로 다시 보기)", EditorStyles.miniButton))
            {
                picked = -1;
            }
        }

        for (int i = 0; i < lanes.Count; i++)
        {
            if (DrawLane(lanes[i], i, i == chosen, routes))
            {
                picked = i;
            }

            if (i == chosen)
            {
                DrawWaits(lanes[i], routes);
            }
        }

        GUILayout.Label("줄을 누르면 그 경로만 남고 나머지는 회색으로 죽습니다. 경로 도구의 편집 대상도 같이 옮겨집니다.",
            EditorStyles.wordWrappedMiniLabel);
        DrawGuide();

        return picked;
    }

    // 경로 도구 사용법. 처음 여는 사람이 문서를 찾아가지 않아도 되게 쓰는 자리에 둔다.
    private static void DrawGuide()
    {
        EditorGUILayout.HelpBox(
            "경로 그리기 — 위 도구 줄에서 [경로]를 고릅니다.\n\n" +
            "· 그리기 : 스폰 칸을 누른 채 본진까지 끕니다. 지나간 칸이 그대로 경로가 됩니다.\n" +
            "· 같은 칸을 몇 번이고 다시 지나가도 됩니다 — 뱅글 도는 경로를 만들 수 있습니다.\n" +
            "· 스폰을 끌지 않고 누르기만 하면 그 경로를 고르기만 하고 지우지 않습니다.\n\n" +
            "· 점 찍기 : 한 칸씩 눌러 맨 뒤에 쌓습니다. Alt = 뒤로, Ctrl = 들어갈 자리 자동.\n\n" +
            "· [뒤로] : 마지막에 그린 칸부터 하나씩 빠집니다. Ctrl+Z로도 됩니다.\n" +
            "· [비우기] : 그린 것을 다 지웁니다 — 그 경로는 다시 자동 최단 경로가 됩니다.\n" +
            "· [← 전체 보기] : 고른 것을 놓고 모든 경로를 다시 봅니다.\n" +
            "· 끌다 만 나머지는 본진까지 자동으로 이어집니다.\n" +
            "· 지나갈 수 없는 칸에는 선이 안 들어갑니다 — 벽 앞에서 멈춥니다.",
            MessageType.None);
    }

    // 줄 전체가 버튼이다 — 작은 버튼 하나만 누를 수 있으면 고르는 자리를 겨냥해야 한다.
    private static bool DrawLane(LaneData lane, int index, bool chosen, RouteConfig routes)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            // 격자에 그려진 선과 같은 색을 찍는다 — 이 점이 줄과 선을 잇는 유일한 단서다.
            Rect mark = GUILayoutUtility.GetRect(11f, 13f, GUILayout.Width(11));
            EditorGUI.DrawRect(new Rect(mark.x, mark.y + 2f, 9f, 9f),
                lane.IsValid ? MapMakerPalette.Lane(index) : MapMakerPalette.Problem);

            int nodes = NodeCount(lane, routes);
            GUIStyle style = chosen ? EditorStyles.miniBoldLabel : EditorStyles.miniLabel;
            bool hit = GUILayout.Button(Word(lane, nodes), style);

            using (new EditorGUI.DisabledScope(nodes == 0))
            {
                // 뒤로가기는 맨 뒤 한 칸씩. 마지막에 그린 것부터 빠지므로 손이 기억하는 순서와 같다.
                if (GUILayout.Button("뒤로", EditorStyles.miniButton, GUILayout.Width(34)))
                {
                    RouteEdit.PopNode(routes, routes.IndexOf(lane.Route));
                    return true; // 줄어든 경로를 바로 보게 고른 상태로 넘긴다
                }

                if (GUILayout.Button("비우기", EditorStyles.miniButton, GUILayout.Width(44)))
                {
                    RouteEdit.ClearNodes(routes, routes.IndexOf(lane.Route));
                    return true;
                }
            }

            return hit;
        }
    }

    // 고른 경로의 경유 칸을 한 줄씩 펼쳐 멈출 초를 받는다. 0초는 멈추지 않는 칸이다.
    private static void DrawWaits(LaneData lane, RouteConfig routes)
    {
        if (NodeCount(lane, routes) == 0)
        {
            return;
        }

        RouteData route = lane.Route;

        for (int i = 0; i < route.Nodes.Count; i++)
        {
            DrawWait(routes, route, i);
        }
    }

    // 경유 칸 한 줄. 초를 고쳐 넣은 프레임에만 저작에 적어 되돌리기가 한 번에 하나씩 쌓이게 한다.
    private static void DrawWait(RouteConfig routes, RouteData route, int slot)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(14f);
            GUILayout.Label($"{slot + 1}. {route.Nodes[slot].Coord}", EditorStyles.miniLabel);

            EditorGUI.BeginChangeCheck();
            float seconds = EditorGUILayout.FloatField(route.Nodes[slot].WaitTime, GUILayout.Width(40));

            if (EditorGUI.EndChangeCheck())
            {
                RouteEdit.SetWait(routes, routes.IndexOf(route), slot, seconds);
            }

            GUILayout.Label("초", EditorStyles.miniLabel, GUILayout.Width(16));
        }
    }

    // 이 스폰에 사람이 찍어 둔 경유 칸 수. 저작이 없으면 0(자동 최단 경로다).
    private static int NodeCount(LaneData lane, RouteConfig routes)
    {
        if (routes == null || lane.Start == null)
        {
            return 0;
        }

        if (lane.Route == null)
        {
            return 0;
        }

        return lane.Route.Nodes.Count;
    }

    private static string Word(LaneData lane, int nodes)
    {
        string from = lane.Start != null ? lane.Start.Coord.ToString() : "(스폰 없음)";
        if (!lane.IsValid)
        {
            return $"스폰 {from} · 막힘 — 본진까지 가는 길이 없습니다";
        }

        if (nodes == 0)
        {
            return $"스폰 {from} · {lane.Tiles.Count}칸 · 자동";
        }

        return $"스폰 {from} · {lane.Tiles.Count}칸 · 그린 {nodes}칸";
    }
}
