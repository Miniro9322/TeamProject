using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EnemyInfo : MonoBehaviour
{
    public TMP_Text e_NameText;
    public TMP_Text e_DescText;
    public TMP_Text e_SkillNameText;
    public TMP_Text e_SkillDescText;
    public void Info(EnemyTable.Data data)
    {
        if (data == null) return;
        var st = DataTableManager.StringTable;
        e_NameText.text = st.Get(data.Name);
        e_DescText.text = st.Get(data.Desc);

        if (string.IsNullOrEmpty(data.Skills) ||
            data.Skills.Equals("None", System.StringComparison.OrdinalIgnoreCase))
        {
            e_SkillNameText.text = string.Empty;
            e_SkillDescText.text = string.Empty;
            return;
        }

        var skillTable = DataTableManager.SkillTable;
        var names = new List<string>();
        var descs = new List<string>();
        foreach (var raw in data.Skills.Split(';'))
        {
            var id = raw.Trim();
            if (string.IsNullOrEmpty(id)) continue;
            var skill = skillTable.Get(id);
            if (skill == null) continue;
            names.Add(st.Get(skill.NameKey));
            descs.Add(st.Get(skill.Desc));
        }
        e_SkillNameText.text = string.Join("\n", names);
        e_SkillDescText.text = string.Join("\n", descs);
    }
}
