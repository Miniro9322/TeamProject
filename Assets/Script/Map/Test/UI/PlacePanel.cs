using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class PlacePanel : MonoBehaviour
{
    [SerializeField] private MapView view;
    [SerializeField] private PlacePalette palette;

    public void Show(bool isHero)
    {
        Clear();
        for (int i = 0; i < palette.Slots.Count; i++)
        {
            bool hero = palette.Slots[i].kind == OccupantKind.MeleeHero
                     || palette.Slots[i].kind == OccupantKind.RangedHero;
            if (hero != isHero) continue;
            int idx = i;
            MakeButton(palette.Slots[i].label, () => view.SetUnit(idx));
        }
    }

    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
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
