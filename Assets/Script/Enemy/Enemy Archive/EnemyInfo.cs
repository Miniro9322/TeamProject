using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
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

    // 미해금일 때 e_Image에 띄울 ? 스프라이트. 목록 버튼(EnemyArchiveButton)과 같은 것을 쓰려고
    // 인스펙터에 또 꽂지 않고 EnemyArchive가 SetLockIcon으로 넘겨 준다 — 출처를 하나로 둔다.
    private Sprite lockIcon;

    /// <summary>목록이 쓰는 ? 스프라이트를 상세 패널에도 알려 준다. EnemyArchive가 적을 띄울 때마다 호출한다.</summary>
    public void SetLockIcon(Sprite icon) => lockIcon = icon;

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
    public Color burrowColor       = new Color(0.65f, 0.32f, 0.20f); // 잠행 - 적갈색
    public Color FlameColor = new Color(0.96f,0.12f,0.12f);

    [Tooltip("고유 특성으로 표기한 스킬의 글자색. 특성 단어와 눈으로 구분되게 다른 색을 주는 게 좋다.")]
    public Color signatureSkillColor = new Color(0.95f, 0.55f, 0.85f); // 고유 스킬 - 분홍

    [Header("책 펼침 연출")]
    [Tooltip("책 펼침 애니메이터(BookAnimatorCon). 비워두면 트리거를 쏘지 않고 페이드만 한다.")]
    [SerializeField] private Animator bookAnimator;
    [Tooltip("펼침을 시작시킬 트리거 이름. BookAnimatorCon의 파라미터와 같아야 한다.")]
    [SerializeField] private string openTrigger = "Open";
    [Tooltip("페이드할 대상. 비워두면 텍스트들과 적 아이콘(e_Image)의 alpha를 직접 건드린다 — " +
             "컴포넌트 추가 없이 바로 동작한다. 테두리·배경까지 통째로 페이드하려면 그것들을 묶은 부모에 " +
             "CanvasGroup을 붙여 여기 꽂는다(그 경우 아래 개별 alpha는 건드리지 않는다).")]
    [SerializeField] private CanvasGroup textGroup;
    [Tooltip("펼침 시작 후 텍스트가 뜨기 시작할 시각(초). 펼침 클립 길이의 8할쯤으로 맞추면 '다 펼쳐질 때쯤' 뜬다. " +
             "클립에 Animation Event로 AnimEvent_BookOpened를 걸어두면 그게 먼저 걸리고 이 값은 안전망(타임아웃)으로만 쓰인다.")]
    [SerializeField] private float revealDelay = 0.5f;
    [Tooltip("서서히 뜨는 데 걸리는 시간(초). 0이면 즉시 표시(연출 없이 기존 거동).")]
    [SerializeField] private float fadeDuration = 0.35f;

    // 진행 중인 페이드. 다른 적을 연달아 누르면 앞선 페이드를 끊고 처음부터 다시 한다.
    private CancellationTokenSource revealCts;
    // Animation Event가 왔는지. revealDelay는 이벤트를 안 걸었을 때를 위한 안전망이다
    // (EnemyBurrow가 파고들기 이벤트에 타임아웃을 함께 두는 것과 같은 구조).
    private bool bookOpenedEvent;

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
        ApplyTitle();
        Clear();
    }

    void OnEnable()
    {
        // 이 패널의 문구는 코드가 직접 채우므로 LocalizeText가 붙지 않는다.
        // 언어를 바꿔도 갱신되지 않으니 여기서 직접 이벤트를 구독한다.
        LocalizeTextManager.OnLanguageChanged += Relocalize;
    }

    void OnDisable()
    {
        LocalizeTextManager.OnLanguageChanged -= Relocalize;
        Clear();
    }

    private void ApplyTitle()
    {
        SetText(m_ArchiveText, DataTableManager.StringTable.Get("Ui_EnemyArchive"));
    }

    // 언어가 바뀌면 캐시해둔 문자열을 버리고 지금 보고 있던 적으로 다시 만든다.
    // Info()가 page를 0으로 되돌리므로 보고 있던 페이지는 따로 보존한다.
    private void Relocalize()
    {
        ApplyTitle();
        if (current == null) return;
        // 잠금 화면은 lockedName/lockedMessage(인스펙터 문자열)라 StringTable과 무관 — 다시 그릴 것이 없다.
        // 여기서 Info()를 부르면 ShowLocked()가 다시 돌아 잠금 팝업만 또 뜬다.
        if (!EnemyArchiveData.IsUnlocked(current.Name)) return;

        int keepPage = page;
        Info(current, playOpenAnimation: false); // 언어만 바뀐 것이라 책을 다시 펼치지 않는다
        page = Mathf.Clamp(keepPage, 0, LastPage);
        Show();
    }

    // 지금 표시 중인 적. 언어 전환 시 이걸로 문자열을 다시 만든다.
    private EnemyTable.Data current;

    // 처음엔 비워둔 상태로 시작
    public void Clear()
    {
        CancelReveal();
        SetRevealAlpha(1f);   // 페이드 도중 닫혀도 다음에 열 때 글자가 투명하게 남아 있지 않게
        current = null;
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

    public void Info(EnemyTable.Data data) => Info(data, true);

    /// <summary>playOpenAnimation=false면 책을 다시 펼치지 않고 글자만 갈아끼운다(언어 전환 등).</summary>
    public void Info(EnemyTable.Data data, bool playOpenAnimation)
    {
        if (data == null) { Clear(); return; }

        current = data; // 언어 전환 시 다시 그릴 대상(미해금이어도 기억해둬야 잠금 문구가 갱신된다)

        // 해금 여부와 무관하게 책은 펼쳐진다 — 잠금 문구도 같이 서서히 떠야 자연스럽다.
        if (playOpenAnimation) PlayOpenAndReveal();

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
        if (e_Image != null)
        {
            // 목록 버튼과 같은 ? 이미지를 띄운다. 안 꽂혀 있으면(null) 예전처럼 그냥 숨긴다.
            e_Image.sprite = lockIcon;
            e_Image.enabled = lockIcon != null;
        }
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

    // data.Attribute 원본("Fly|Cloaking|FireZoneSkill" 등)을 파싱해 각 항목을 번역·결합. 없으면 "특성 없음"(None).
    //
    // EnemyAttribute로 파싱되는 토큰은 특성, 아니면 SkillTable의 스킬 ID로 본다 —
    // "고유 특성으로 이 스킬을 가진다"를 특성 줄에 같이 세우기 위함이다.
    // 표시 순서는 CSV에 적은 순서를 그대로 따른다(enum 선언 순서가 아니다) — 저작자가 순서를 쥔다.
    //
    // 이 칸은 표시 전용이다. 적이 실제로 그 스킬을 쓰는지는 EnemyTable의 Skills 칸이 정하고,
    // EnemyBase.ParseAttribute는 여기 적힌 스킬 ID를 (enum이 아니므로) 조용히 무시한다.
    private string LocalizeAttributes(string raw)
    {
        var st = DataTableManager.StringTable;
        var parts = new List<string>();
        var seen = new HashSet<string>();   // 같은 토큰을 두 번 적어도 한 번만 표기

        if (!string.IsNullOrEmpty(raw))
        {
            foreach (string token in raw.Split(new[] { '|', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string id = token.Trim();
                if (id.Length == 0 || !seen.Add(id)) continue;

                if (Enum.TryParse(id, true, out EnemyAttribute flag))
                {
                    if (flag == EnemyAttribute.None) continue;   // "None"을 적은 경우 — 아래 폴백이 처리한다
                    // 색 입히고 <link>로 감싼다 → AttributeTooltip이 hover 감지. link ID = 특성 enum 이름
                    parts.Add($"<link=\"{flag}\">{Wrap(st.Get(flag.ToString()), AttrColor(flag))}</link>");
                    continue;
                }

                SkillTable.Data skill = DataTableManager.SkillTable?.Get(id);
                if (skill == null)
                {
                    // 예전엔 조용히 사라져서 오타를 못 잡았다. 특성도 스킬도 아니면 알려준다.
                    Debug.LogWarning($"EnemyInfo: 특성 칸의 '{id}'는 EnemyAttribute도 스킬 ID도 아니라 건너뛴다.", this);
                    continue;
                }
                // link ID = 스킬 ID. 툴팁이 SkillTable에서 그 행의 Desc 키를 찾아 준다.
                // NameKey가 빈 칸이면 null이고 StringTable.Get(null)은 예외를 던지므로 스킬 ID를 그대로 보여준다.
                string label = string.IsNullOrEmpty(skill.NameKey) ? id : st.Get(skill.NameKey);
                parts.Add($"<link=\"{id}\">{Wrap(label, signatureSkillColor)}</link>");
            }
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
            case EnemyAttribute.Burrow: return burrowColor;
            default:                          return Color.white;
        }
    }

    // TMP 리치 텍스트 color 태그로 감싸기
    private static string Wrap(string text, Color c)
        => $"<color=#{ColorUtility.ToHtmlStringRGB(c)}>{text}</color>";

    // ---- 책 펼침 + 텍스트 페이드 ----

    /// <summary>펼침 클립 마지막 프레임에 Animation Event로 이 이름을 걸면 정확한 타이밍에 텍스트가 뜬다.
    /// 안 걸어도 revealDelay가 안전망으로 대신 띄운다(EnemyBase의 AnimEvent_Burrowed와 같은 구조).</summary>
    public void AnimEvent_BookOpened() => bookOpenedEvent = true;

    // 책을 펼치고, 다 펼쳐질 때쯤 텍스트를 서서히 띄운다.
    private void PlayOpenAndReveal()
    {
        CancelReveal();
        bookOpenedEvent = false;

        if (bookAnimator != null)
        {
            // 같은 트리거가 큐에 남아 있으면 다음 펼침이 즉시 소비돼 버린다 — 눌러 둔 것을 먼저 지운다.
            bookAnimator.ResetTrigger(openTrigger);
            bookAnimator.SetTrigger(openTrigger);
        }

        SetRevealAlpha(0f);   // 펼치는 동안은 텍스트·아이콘을 감춰 둔다
        revealCts = new CancellationTokenSource();
        RevealAfterOpen(revealCts.Token).Forget();
    }

    private void CancelReveal()
    {
        revealCts?.Cancel();
        revealCts?.Dispose();
        revealCts = null;
    }

    private async UniTask RevealAfterOpen(CancellationToken token)
    {
        // 도감은 Time.timeScale이 0인 동안에도 열리므로(일시정지 중 확인) 전부 unscaled로 잰다.
        // 같은 이유로 bookAnimator의 Update Mode도 Unscaled Time이어야 펼침 애니가 멈추지 않는다.
        float t = 0f;
        while (!bookOpenedEvent && t < revealDelay)
        {
            t += Time.unscaledDeltaTime;
            await UniTask.Yield(token);
        }

        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            SetRevealAlpha(Mathf.Clamp01(t / fadeDuration));
            await UniTask.Yield(token);
        }
        SetRevealAlpha(1f);
    }

    // textGroup이 꽂혀 있으면 그쪽 alpha 하나로, 없으면 텍스트들과 적 아이콘의 alpha를 직접 건드린다.
    // 도감 제목(m_ArchiveText)은 책과 무관하게 항상 보여야 하므로 건드리지 않는다.
    private void SetRevealAlpha(float a)
    {
        if (textGroup != null) { textGroup.alpha = a; return; }

        SetAlpha(e_NameText, a);
        SetAlpha(e_TypeText, a);
        SetAlpha(e_Attribute, a);
        SetAlpha(e_DescText, a);
        SetAlpha(e_SkillNameText, a);
        SetAlpha(e_SkillDescText, a);
        // 아이콘은 enabled로 켜고 끄는 것과 별개로 색 alpha만 조절한다 —
        // Info가 sprite 유무로 enabled를 정한 뒤에도 이 값이 그대로 남아 같은 타이밍에 떠오른다.
        SetAlpha(e_Image, a);
    }

    private static void SetAlpha(TMP_Text t, float a)
    {
        if (t != null) t.alpha = a;
    }

    private static void SetAlpha(Image img, float a)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = a;
        img.color = c;
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
