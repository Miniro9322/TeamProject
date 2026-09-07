using TMPro;
using UnityEngine;

public class PlaceModeStatusText : MonoBehaviour
{
    [SerializeField] private MapView view;
    [SerializeField] private TextMeshProUGUI text;

    private void OnEnable()
    {
        view.OnStateChanged += Refresh;
        LocalizeTextManager.OnLanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        view.OnStateChanged -= Refresh;
        LocalizeTextManager.OnLanguageChanged -= Refresh;
    }

    private void Refresh()
    {
        var stringTable = DataTableManager.StringTable;
        if (view.IsRemoving)
        {
            text.text = stringTable.Get("Ui_PlaceModeRemove");
        }
        else if (view.IsReplacing)
        {
            text.text = stringTable.Get(view.IsHolding ? "Ui_PlaceModeReplaceHold" : "Ui_PlaceModeReplace");
        }
        else
        {
            text.text = string.Empty;
        }
    }
}
