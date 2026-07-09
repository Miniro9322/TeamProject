using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Dropdown))]
public class LocalizeDropdown : MonoBehaviour
{
    [SerializeField] private List<string> keys = new List<string>();
    private TMP_Dropdown dropdown;

    private void Awake()
    {
        dropdown = GetComponent<TMP_Dropdown>();
    }

    private void OnEnable()
    {
        LocalizeTextManager.OnLanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        LocalizeTextManager.OnLanguageChanged -= Refresh;
    }

    public void SetKeys(List<string> newKeys)
    {
        keys = newKeys;
        Refresh();
    }

    public void Refresh()
    {
        if (dropdown == null) dropdown = GetComponent<TMP_Dropdown>();
        if (keys == null || keys.Count == 0) return;

        var stringTable = DataTableManager.Get<StringTable>(DataTableIds.String);
        if (stringTable == null)
        {
            Debug.LogWarning("LocalizeDropdown: StringTable을 찾을 수 없음");
            return;
        }

        var options = new List<TMP_Dropdown.OptionData>(keys.Count);
        foreach (var key in keys)
        {
            options.Add(new TMP_Dropdown.OptionData(stringTable.Get(key)));
        }

        int prevValue = dropdown.value;
        dropdown.options = options;
        dropdown.SetValueWithoutNotify(Mathf.Clamp(prevValue, 0, options.Count - 1));
        dropdown.RefreshShownValue();
    }
}
