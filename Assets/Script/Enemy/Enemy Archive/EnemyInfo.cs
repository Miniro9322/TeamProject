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
    public TMP_Text e_NameText;     
    public TMP_Text e_DescText;      
    public TMP_Text e_SkillNameText; 
    public TMP_Text e_SkillDescText;   
    public Button leftArrowButton;     
    public Button rightArrowButton;    

    [Header("미해금(아직 못 만난 적) 표시")]
    public string lockedName = "???";
    [TextArea] public string lockedMessage = "???";
    public GameObject lockedEnemyText;

    private string enemyName;
    private string enemyDesc;
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
        skillNames.Clear();
        skillDescs.Clear();
        page = 0;
        SetText(e_NameText, string.Empty);
        SetText(e_DescText, string.Empty);
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

    private static void SetText(TMP_Text t, string value)
    {
        if (t != null) t.text = value;
    }

    private static void SetActive(Button b, bool on)
    {
        if (b != null) b.gameObject.SetActive(on);
    }
}
