using System;
using UnityEngine;

public class MapPanel : MonoBehaviour
{
    [Header("패널 크기")]
    [SerializeField, Min(200f)] private float panelWidth = 360f;
    [SerializeField, Min(100f)] private float infoHeight = 420f;
    [SerializeField, Min(100f)] private float testHeight = 760f;
    [SerializeField, Min(0f)] private float panelMargin = 20f;

    [Header("패널 스타일")]
    [SerializeField, Range(12, 32)] private int fontSize = 18;
    [SerializeField] private GUISkin panelSkin;

    private PanelData data;

    public bool HasPointer { get; private set; }

    public event Action PathClicked;
    public event Action PathToggle;
    public event Action UnitCleared;
    public event Action RemoveClicked;
    public event Action ModeCleared;
    public event Action<int> UnitClicked;
    public event Action ReplaceClicked;
    public event Action ModuleUnlockClicked;

    public void SetData(PanelData value)
    {
        data = value;
    }

    private void OnGUI()
    {
        if (data == null)
        {
            HasPointer = false;
            return;
        }

        GUISkin oldSkin = GUI.skin;
        if (panelSkin != null)
        {
            GUI.skin = panelSkin;
        }

        int labelSize = GUI.skin.label.fontSize;
        int buttonSize = GUI.skin.button.fontSize;
        GUI.skin.label.fontSize = fontSize;
        GUI.skin.button.fontSize = fontSize;

        Rect info = new(panelMargin, panelMargin, panelWidth, infoHeight);
        Rect test = new(
            Screen.width - panelWidth - panelMargin,
            panelMargin,
            panelWidth,
            testHeight);
        HasPointer = info.Contains(Event.current.mousePosition) || test.Contains(Event.current.mousePosition);

        DrawInfo(info);
        DrawTest(test);

        GUI.skin.label.fontSize = labelSize;
        GUI.skin.button.fontSize = buttonSize;
        GUI.skin = oldSkin;
    }

    private void DrawInfo(Rect rect)
    {
        GUILayout.BeginArea(rect, GUI.skin.box);
        GUILayout.Label("맵 정보");
        GUILayout.Label(data.Status);

        if (!string.IsNullOrEmpty(data.TileText))
        {
            GUILayout.Space(4f);
            GUILayout.Label(data.TileText);
        }

        GUILayout.EndArea();
    }

    private void DrawTest(Rect rect)
    {
        GUILayout.BeginArea(rect, GUI.skin.box);
        GUILayout.Label("맵 테스트");

        GUI.enabled = !data.Holding; // 유닛 집은 이동 중에는 버튼 비활성화(회색·클릭 불가).
        DrawPath();
        DrawUnits();
        GUI.enabled = true;          // 이후 GUI에 영향 없도록 원복.

        GUILayout.EndArea();
    }

    private void DrawPath()
    {
        GUILayout.Label($"경로 표시: {(data.ShowPath ? "ON" : "OFF")}");
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("경로 재계산"))
        {
            PathClicked?.Invoke();
        }

        if (GUILayout.Button(data.ShowPath ? "경로 끄기" : "경로 켜기"))
        {
            PathToggle?.Invoke();
        }

        GUILayout.EndHorizontal();
    }

    private void DrawUnits()
    {
        GUILayout.Space(6f);
        GUILayout.Label($"배치 모드: {data.Mode}");

        for (int i = 0; i < data.Units.Length; i++)
        {
            string mark = data.UnitIndex == i ? "> " : "";
            if (GUILayout.Button($"{mark}{data.Units[i]}"))
            {
                UnitClicked?.Invoke(i);
            }
        }

        if (GUILayout.Button("재배치"))
        {
            ReplaceClicked?.Invoke();
        }

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("제거"))
        {
            RemoveClicked?.Invoke();
        }

        if (GUILayout.Button("끄기"))
        {
            ModeCleared?.Invoke();
        }

        GUILayout.EndHorizontal();

        if (GUILayout.Button("배치 전부 제거"))
        {
            UnitCleared?.Invoke();
        }
    }
}
