using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 도감 상세 패널.
//  - page 0    : 적 이름 + 적 설명 (스킬 텍스트는 비움)
//  - page 1..N : 적 설명은 비우고, 스킬 이름/설명 텍스트에 각 스킬을 표시
// 왼쪽/오른쪽 화살표로 페이지를 이동한다(순환 없음):
//  - 첫 페이지(0)면 왼쪽 화살표 숨김, 마지막 페이지면 오른쪽 화살표 숨김, 가운데면 둘 다 표시.
public class EnemyInfo : MonoBehaviour
{
    public Image e_Image;
    public TMP_Text m_ArchiveText;
    public TMP_Text e_NameText;
    public TMP_Text e_TypeText;
    public TMP_Text e_DescText;     
    public TMP_Text e_Attribute; 
    public TMP_Text e_SkillNameText; 
    public TMP_Text e_SkillDescText;
    public Button leftArrowButton;     
    public Button rightArrowButton;    

    [Header("미해금(아직 못 만난 적) 표시")]
    public string lockedName = "???";
    [TextArea] public string lockedMessage = "???";
    public GameObject lockedEnemyText;

    [Header("특성 표시")]
    [Tooltip("특성이 여러 개일 때 구분자 (예: \", \" 또는 \" | \")")]
    public string attributeSeparator = ", ";

    [Header("특성별 글자색 (TMP Rich Text 필요)")]
    public Color cloakingColor     = new Color(0.61f, 0.35f, 0.71f); // 은신 - 보라
    public Color flyColor          = new Color(0.20f, 0.60f, 0.86f); // 공중 - 하늘
    public Color unJudgedColor     = new Color(0.90f, 0.49f, 0.13f); // 저지 불가 - 주황
    public Color berserkColor      = new Color(0.91f, 0.30f, 0.24f); // 폭주 - 빨강
    public Color regenerationColor = new Color(0.18f, 0.80f, 0.44f); // 재생 - 초록
    public Color hitsShieldColor   = new Color(0.95f, 0.77f, 0.06f); // 타수 보호막 - 노랑

    private string enemyName;
    private string enemyDesc;
    private string enemyType;
    private string enemyAttribute;
    private readonly List<string> skillNames = new();
    private readonly List<string> skillDescs = new();
    private int page;
    private int LastPage => skillDescs.Count;

    void Awake()
    {
        if (leftArrowButton != null)
        {
            leftArrowButton.onClick.RemoveAllListeners();
            leftArrowButton.onClick.AddListener(PrevPage);
        }
        if (rightArrowButton != null)
        {
            rightArrowButton.onClick.RemoveAllListeners();
            rightArrowButton.onClick.AddListener(NextPage);
        }
        m_ArchiveText.text = $"{DataTableManager.StringTable.Get("Ui_EnemyArchive")}";
        Clear();
    }
    void OnDisable()
    {
        Clear();
    }

    // 처음엔 비워둔 상태로 시작
    public void Clear()
    {
        enemyName = string.Empty;
        enemyDesc = string.Empty;
        enemyType = string.Empty;
        enemyAttribute = string.Empty;
        skillNames.Clear();
        skillDescs.Clear();
        page = 0;
        SetText(e_NameText, string.Empty);
        SetText(e_DescText, string.Empty);
        SetText(e_TypeText, string.Empty);
        SetText(e_Attribute, string.Empty);
        SetText(e_SkillNameText, string.Empty);
        SetText(e_SkillDescText, string.Empty);
        if (e_Image != null) e_Image.enabled = false;
        SetActive(leftArrowButton, false);
        SetActive(rightArrowButton, false);
    }

    public void Info(EnemyTable.Data data)
    {
        if (data == null) { Clear(); return; }

        if (!EnemyArchiveData.IsUnlocked(data.Name)) { ShowLocked(); return; }

        var st = DataTableManager.StringTable;

        enemyName = st.Get(data.Name);
        enemyDesc = st.Get(data.Desc);
        enemyType = $"{st.Get("Ui_Type")} : {st.Get(data.Type)}";
        enemyAttribute = $"{st.Get("Ui_Attribute")} : {LocalizeAttributes(data.Attribute)}";
        skillNames.Clear();
        skillDescs.Clear();
        if (!string.IsNullOrEmpty(data.Skills) &&
            !data.Skills.Equals("None", System.StringComparison.OrdinalIgnoreCase))
        {
            var skillTable = DataTableManager.SkillTable;
            foreach (var raw in data.Skills.Split(';'))
            {
                var id = raw.Trim();
                if (string.IsNullOrEmpty(id)) continue;
                var skill = skillTable.Get(id);
                if (skill == null) continue;
                skillNames.Add(st.Get(skill.NameKey));
                skillDescs.Add(st.Get(skill.Desc));
            }
        }

        if (e_Image != null)
        {
            e_Image.sprite = Resources.Load<Sprite>($"EnemyIcons/{data.Name}");
            e_Image.enabled = e_Image.sprite != null;
        }
        page = 0;
        Show();
    }
    private void ShowLocked()
    {
        page = 0;
        skillNames.Clear();
        skillDescs.Clear();
        GameObject go = PoolManager.Instance.Spawn(lockedEnemyText,transform.position,Quaternion.identity,gameObject.transform);
        PoolManager.Instance.Despawn(go,1f);
        SetText(e_NameText, lockedName);
        SetText(e_DescText, lockedMessage);
        SetText(e_TypeText, string.Empty);
        SetText(e_Attribute, string.Empty);
        SetText(e_SkillNameText, string.Empty);
        SetText(e_SkillDescText, string.Empty);
        if (e_Image != null) e_Image.enabled = false;
        SetActive(leftArrowButton, false);
        SetActive(rightArrowButton, false);
    }

    private void NextPage()
    {
        if (page >= LastPage) return;
        page++;
        Show();
    }

    private void PrevPage()
    {
        if (page <= 0) return;
        page--;
        Show();
    }

    private void Show()
    {
        SetText(e_NameText, enemyName);   // 적 이름은 항상 유지
        SetText(e_TypeText, enemyType);
        SetText(e_Attribute, enemyAttribute);
        if (page == 0)
        {
            SetText(e_DescText, enemyDesc);
            SetText(e_SkillNameText, string.Empty);
            SetText(e_SkillDescText, string.Empty);
        }
        else
        {
            int i = page - 1;
            SetText(e_DescText, string.Empty);          // 기존(적) 설명 비움
            SetText(e_SkillNameText, skillNames[i]);
            SetText(e_SkillDescText, skillDescs[i]);
        }

        // 왼쪽: 뒤로 갈 페이지 있을 때 / 오른쪽: 넘길 페이지 있을 때
        SetActive(leftArrowButton, page > 0);
        SetActive(rightArrowButton, page < LastPage);
    }

    // data.Attribute 원본("Fly|Cloaking" 등)을 파싱해 각 특성을 번역·결합. 없으면 "특성 없음"(None).
    private string LocalizeAttributes(string raw)
    {
        var st = DataTableManager.StringTable;
        EnemyAttribute attr = ParseAttribute(raw);

        var parts = new List<string>();
        foreach (EnemyAttribute f in Enum.GetValues(typeof(EnemyAttribute)))
            if (f != EnemyAttribute.None && (attr & f) != 0)
            {
                // 색 입히고 <link>로 감싼다 → AttributeTooltip이 hover 감지. link ID = 특성 enum 이름
                string colored = Wrap(st.Get(f.ToString()), AttrColor(f));
                parts.Add($"<link=\"{f}\">{colored}</link>");
            }

        return parts.Count > 0 ? string.Join(attributeSeparator, parts) : st.Get("None");
    }

    // 특성별 글자색
    private Color AttrColor(EnemyAttribute f)
    {
        switch (f)
        {
            case EnemyAttribute.Cloaking:     return cloakingColor;
            case EnemyAttribute.Fly:          return flyColor;
            case EnemyAttribute.UnJudged:     return unJudgedColor;
            case EnemyAttribute.Berserk:      return berserkColor;
            case EnemyAttribute.Regeneration: return regenerationColor;
            case EnemyAttribute.HitsShield:   return hitsShieldColor;
            default:                          return Color.white;
        }
    }

    // TMP 리치 텍스트 color 태그로 감싸기
    private static string Wrap(string text, Color c)
        => $"<color=#{ColorUtility.ToHtmlStringRGB(c)}>{text}</color>";

    private static EnemyAttribute ParseAttribute(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return EnemyAttribute.None;
        EnemyAttribute result = EnemyAttribute.None;
        foreach (var token in raw.Split(new[] { '|', ';' }, StringSplitOptions.RemoveEmptyEntries))
            if (Enum.TryParse(token.Trim(), true, out EnemyAttribute flag))
                result |= flag;
        return result;
    }

    private static void SetText(TMP_Text t, string value)
    {
        if (t != null) t.text = value;
    }

    private static void SetActive(Button b, bool on)
    {
        if (b != null) b.gameObject.SetActive(on);
    }
}
