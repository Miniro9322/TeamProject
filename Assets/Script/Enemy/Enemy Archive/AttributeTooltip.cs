using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// 특성 텍스트(e_Attribute)의 특성 단어(<link>)에 마우스를 hoverDelay초 이상 올리면
// 그 특성 설명 툴팁을 마우스 옆에 띄운다.
//  - 특성 단어는 EnemyInfo가 <link="EnemyAttribute이름">...</link> 로 감싸준다.
//  - 설명 문구는 StringTable의 "<특성이름>_Desc" 키에서 가져온다.
public class AttributeTooltip : MonoBehaviour
{
    [SerializeField] private TMP_Text attributeText;   // 특성이 표시되는 TMP (e_Attribute)
    [SerializeField] private GameObject tooltip;       // 툴팁 패널 (켜고 끔)
    [SerializeField] private TMP_Text tooltipText;     // 툴팁 안 설명 텍스트
    [SerializeField] private float hoverDelay = 1f;    // 몇 초 머무르면 뜨는지

    private Camera uiCamera;   // 오버레이 캔버스면 null
    private int lastLink = -1; // 현재 머무르는 link 인덱스
    private float hoverTimer;
    private bool shown;

    void Awake()
    {
        Hide();
        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = canvas.worldCamera;
    }

    void OnDisable() => ResetHover();

    void Update()
    {
        if (attributeText == null || Mouse.current == null) { ResetHover(); return; }

        Vector2 mouse = Mouse.current.position.ReadValue();
        int link = TMP_TextUtilities.FindIntersectingLink(attributeText, mouse, uiCamera);

        if (link == -1)          // 특성 단어 위가 아님
        {
            ResetHover();
            return;
        }

        if (link != lastLink)    // 다른 특성으로 이동 → 타이머 리셋
        {
            lastLink = link;
            hoverTimer = 0f;
            if (shown) Hide();
        }

        hoverTimer += Time.unscaledDeltaTime;
        if (!shown && hoverTimer >= hoverDelay)
            ShowFor(link);
    }

    private void ShowFor(int linkIndex)
    {
        string id = attributeText.textInfo.linkInfo[linkIndex].GetLinkID();   // 예: "Cloaking"
        if (tooltipText != null) tooltipText.text = DataTableManager.StringTable.Get($"{id}_Desc");
        if (tooltip != null) tooltip.SetActive(true);   // 위치는 씬/인스펙터에 고정해둔 그대로
        shown = true;
    }

    private void ResetHover()
    {
        lastLink = -1;
        hoverTimer = 0f;
        if (shown) Hide();
    }

    private void Hide()
    {
        if (tooltip != null) tooltip.SetActive(false);
        shown = false;
    }
}
