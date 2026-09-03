using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// 적 스탯을 Play Mode 없이 비교하는 창. 입력을 받아 EnemyStatScaling에 계산을 맡기고 표에 넘기기만 한다.
public class EnemySimWindow : EditorWindow
{
    private const string MenuPath = "Tools/Enemy/Enemy Simulator";
    private const string WindowTitle = "Enemy Simulator";
    private const string DayLabel = "일차(Day)";
    private const string ReloadLabel = "에셋 다시 읽기";
    private const string ConditionNote =
        "Play 모드가 아니면 해금 지역 배율은 반영되지 않습니다(1배 고정) · AttackSpeed·이속·사거리는 일차 배율이 없어 원본 그대로 표시";
    private const string MissingTableNote = "적 데이터를 찾지 못했습니다 — Assets/Resources/DataTable/EnemyTable.csv를 확인하세요.";
    private const string IconResourcePath = "EnemyIcons";

    private const int MinimumDayCount = 0;
    private const int MaximumDayCount = 200;
    private const float DayLabelWidth = 96f;

    // 한 줄 = 적 하나. 도감 아이콘(EnemyIconBaker가 구운 Resources/EnemyIcons)까지 같이 들고 있는다.
    private class Row
    {
        public EnemyTable.Data Data;
        public Sprite Icon;
    }

    private List<Row> rows = new();
    private int dayCount;
    private Vector2 scroll;

    // 메뉴에서 창을 연다.
    [MenuItem(MenuPath)]
    public static void OpenWindow()
    {
        GetWindow<EnemySimWindow>(WindowTitle);
    }

    // 창이 켜질 때 적 데이터를 한 번 읽어둔다.
    private void OnEnable()
    {
        ReloadEntries();
    }

    // 프로젝트의 적 데이터와 도감 아이콘을 다시 읽는다.
    private void ReloadEntries()
    {
        rows = new List<Row>();
        List<EnemyTable.Data> entries = HeroSimEnemySource.CollectEntries();
        for (int index = 0; index < entries.Count; index++)
        {
            rows.Add(new Row
            {
                Data = entries[index],
                Icon = Resources.Load<Sprite>($"{IconResourcePath}/{entries[index].Name}"),
            });
        }
    }

    // 화면을 그린다.
    private void OnGUI()
    {
        if (rows.Count == 0)
        {
            EditorGUILayout.HelpBox(MissingTableNote, MessageType.Warning);
            return;
        }

        DrawControls();
        EditorGUILayout.LabelField(ConditionNote, EditorStyles.wordWrappedMiniLabel);
        EnemySimTable.DrawHeader();
        DrawRows();
    }

    // 새로고침 버튼과 일차 슬라이더를 그린다. 일차는 Hero Simulator의 강화 슬라이더와 같은 크기로 키운다.
    private void DrawControls()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(ReloadLabel, EditorStyles.toolbarButton)) ReloadEntries();
        }

        EditorGUIUtility.labelWidth = DayLabelWidth;
        dayCount = EditorGUILayout.IntSlider(DayLabel, dayCount, MinimumDayCount, MaximumDayCount);
    }

    // 적마다 일차 배율을 계산해 표에 늘어놓는다.
    private void DrawRows()
    {
        using var scope = new EditorGUILayout.ScrollViewScope(scroll);
        scroll = scope.scrollPosition;

        for (int index = 0; index < rows.Count; index++)
        {
            Row row = rows[index];
            EnemyStatScaling.Stats stats = EnemyStatScaling.Compute(row.Data, row.Data.Class, dayCount);
            EnemySimTable.DrawRow(row.Data, row.Icon, stats);
        }
    }
}
