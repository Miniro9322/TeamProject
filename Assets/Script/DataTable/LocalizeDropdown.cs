using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Dropdown))]
public class LocalizeDropdown : MonoBehaviour
{
    // Language enum 이름(Kr/En/jp) 앞에 붙여 StringTable 키를 만든다. 예: "Language" + "Kr" = "LanguageKr"
    [SerializeField] private string keyPrefix = "Language";
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

    public void Refresh()
    {
        if (dropdown == null) dropdown = GetComponent<TMP_Dropdown>();

        var stringTable = DataTableManager.Get<StringTable>(DataTableIds.String);
        if (stringTable == null)
        {
            Debug.LogWarning("LocalizeDropdown: StringTable을 찾을 수 없음");
            return;
        }

        // Language enum을 선언(값) 순서대로 옵션에 배치 → 인덱스가 (Language)index와 일치
        var languages = (Language[])Enum.GetValues(typeof(Language));
        var options = new List<TMP_Dropdown.OptionData>(languages.Length);
        foreach (var language in languages)
        {
            var key = keyPrefix + Capitalize(language.ToString());
            options.Add(new TMP_Dropdown.OptionData(stringTable.Get(key)));
        }

        int prevValue = dropdown.value;
        dropdown.options = options;
        dropdown.SetValueWithoutNotify(Mathf.Clamp(prevValue, 0, options.Count - 1));
        dropdown.RefreshShownValue();
    }
    private static string Capitalize(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToUpperInvariant(name[0]) + name.Substring(1);
    }
}
