using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class ModePanel : MonoBehaviour
{
    [SerializeField] private MapView view;
    [SerializeField] private PlacePanel placePanel;

    private void Start()
    {
        MakeButton("시설", () => { view.ClearMode(); placePanel.Show(false); });
        MakeButton("영웅", () => { view.ClearMode(); placePanel.Show(true); });
        MakeButton("재배치", () => { placePanel.Clear(); view.SetReplace(); });
        MakeButton("제거", () => { placePanel.Clear(); view.SetRemove(); });
        MakeButton("끄기", () => { view.ClearMode(); placePanel.Clear(); });
    }

    private void MakeButton(string label, UnityAction action)
    {
        GameObject go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.layer = gameObject.layer;
        go.transform.SetParent(transform, false);

        GameObject txt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txt.layer = gameObject.layer;
        txt.transform.SetParent(go.transform, false);

        RectTransform rt = txt.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = txt.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.black;

        go.GetComponent<Button>().onClick.AddListener(action);
    }
}
