using UnityEditor;
using UnityEngine;

// 계산 결과를 표로 그리는 출력 담당. 아무것도 계산하지 않고 받은 값만 늘어놓는다.
public static class HeroSimTable
{
    private const float NameWidth = 104f;
    private const float TierWidth = 34f;
    private const float StatWidth = 66f;
    private const float DamageWidth = 74f;
    private const float CostWidth = 96f;
    private const float TraitWidth = 150f;
    private const float RowHeight = 17f;

    private const string IntegerFormat = "N0";
    private const string StatFormat = "N1";
    private const string SpeedFormat = "N3";

    private const string NameHeader = "영웅";
    private const string TierHeader = "티어";
    private const string HpHeader = "HP";
    private const string AttackHeader = "ATK";
    private const string DefenceHeader = "DEF";
    private const string SpeedHeader = "AS";
    private const string BlockHeader = "BLK";
    private const string NormalHeader = "평타 1타";
    private const string AverageHeader = "평균 1타";
    private const string DpsHeader = "초당 피해";
    private const string CostHeader = "누적 비용";
    private const string TraitHeader = "트레잇";

    // 표의 머리줄을 그린다.
    public static void DrawHeader()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            DrawCell(NameHeader, NameWidth, EditorStyles.miniBoldLabel);
            DrawCell(TierHeader, TierWidth, EditorStyles.miniBoldLabel);
            DrawCell(HpHeader, StatWidth, EditorStyles.miniBoldLabel);
            DrawCell(AttackHeader, StatWidth, EditorStyles.miniBoldLabel);
            DrawCell(DefenceHeader, StatWidth, EditorStyles.miniBoldLabel);
            DrawCell(SpeedHeader, StatWidth, EditorStyles.miniBoldLabel);
            DrawCell(BlockHeader, TierWidth, EditorStyles.miniBoldLabel);
            DrawCell(NormalHeader, DamageWidth, EditorStyles.miniBoldLabel);
            DrawCell(AverageHeader, DamageWidth, EditorStyles.miniBoldLabel);
            DrawCell(DpsHeader, DamageWidth, EditorStyles.miniBoldLabel);
            DrawCell(CostHeader, CostWidth, EditorStyles.miniBoldLabel);
            DrawCell(TraitHeader, TraitWidth, EditorStyles.miniBoldLabel);
        }
    }

    // 결과 한 줄을 그린다.
    public static void DrawRow(HeroSimResult result)
    {
        using (new EditorGUILayout.HorizontalScope(GUILayout.Height(RowHeight)))
        {
            DrawCell(result.HeroName, NameWidth, EditorStyles.miniBoldLabel);
            DrawCell(result.Tier.ToString(), TierWidth, EditorStyles.miniLabel);
            DrawCell(result.MaxHp.ToString(IntegerFormat), StatWidth, EditorStyles.miniLabel);
            DrawCell(result.AttackPower.ToString(StatFormat), StatWidth, EditorStyles.miniLabel);
            DrawCell(result.Defence.ToString(StatFormat), StatWidth, EditorStyles.miniLabel);
            DrawCell(result.AttackSpeed.ToString(SpeedFormat), StatWidth, EditorStyles.miniLabel);
            DrawCell(result.BlockCount.ToString(IntegerFormat), TierWidth, EditorStyles.miniLabel);
            DrawCell(result.NormalHitDamage.ToString(IntegerFormat), DamageWidth, EditorStyles.miniLabel);
            DrawCell(result.AverageHitDamage.ToString(StatFormat), DamageWidth, EditorStyles.miniLabel);
            DrawCell(result.DamagePerSecond.ToString(IntegerFormat), DamageWidth, EditorStyles.miniLabel);
            DrawCell(result.CumulativeCost.ToString(IntegerFormat), CostWidth, EditorStyles.miniLabel);
            DrawCell(result.TraitNote, TraitWidth, EditorStyles.miniLabel);
        }
    }

    // 칸 하나를 정해진 너비로 그린다.
    private static void DrawCell(string text, float width, GUIStyle style)
    {
        GUILayout.Label(text, style, GUILayout.Width(width));
    }
}
