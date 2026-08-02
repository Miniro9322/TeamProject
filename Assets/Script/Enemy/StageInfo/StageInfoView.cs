using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 구역 클릭 시 포탈 옆에 뜨는 스테이지 정보 팝업(월드 스페이스 캔버스)의 표시 담당.
// 기존에는 TMP_Text 한 개에 "적이름 x 마릿수"를 줄바꿈으로 이어붙였다.
// 이제 적 하나당 StageEnemyRow(아이콘 + x마릿수)를 한 줄씩 만들고,
// 그 줄에 마우스를 올리거나 클릭하면 이름 + 간단한 설명 툴팁을 띄운다.
//
// 프리팹(TextPrefabs.prefab) 루트에 붙인다. WaveSpawner가 Begin()/AddRow()로 채운다.
public class StageInfoView : MonoBehaviour
{
    [Tooltip("행들이 담길 부모 (Vertical Layout Group 권장)")]
    [SerializeField] private Transform rowContainer;
    [Tooltip("적 한 줄 프리팹 (StageEnemyRow 붙은 것)")]
    [SerializeField] private StageEnemyRow rowPrefab;

    [Header("툴팁")]
    [Tooltip("툴팁 패널 (켜고 끔). 행 위에 겹치지 않게 offset으로 밀어준다.")]
    [SerializeField] private GameObject tooltip;
    [SerializeField] private TMP_Text tooltipNameText;
    [SerializeField] private TMP_Text tooltipDescText;
    [Tooltip("툴팁의 '도감 열기' 버튼. 누르면 도감이 열리고 그 적 페이지가 뜬다. 없어도 동작한다.")]
    [SerializeField] private Button archiveButton;
    [Tooltip("마우스를 몇 초 올리고 있으면 뜨는지. 클릭은 이 지연 없이 즉시 뜬다.")]
    [SerializeField] private float hoverDelay = 0.2f;
    [Tooltip("행·툴팁 어디에도 커서가 없을 때 툴팁을 닫기까지의 여유. " +
             "행과 툴팁 사이 빈 공간을 지나 도감 버튼까지 갈 시간을 준다.")]
    [SerializeField] private float hideGrace = 0.3f;
    [Tooltip("툴팁을 행 기준 어디에 띄울지(화면 픽셀). x를 음수로 두면 행의 왼쪽에 뜬다.")]
    [SerializeField] private Vector3 tooltipOffset = new Vector3(120f, 0f, 0f);
    [Tooltip("화면 가장자리에서 최소 이만큼은 띄운다. 툴팁이 화면을 벗어나면 반대쪽으로 넘기거나 안으로 밀어넣는다.")]
    [SerializeField] private float screenMargin = 8f;

    [Header("미해금 처리")]
    [Tooltip("체크하면 도감에서 아직 만나지 않은 적은 이름/설명을 ???로 가린다. " +
             "스테이지 정보는 '오늘 밤 뭐가 오는지' 보고 대비하는 화면이라 기본값은 끔(그대로 공개).")]
    [SerializeField] private bool maskUnknownEnemy = false;
    [SerializeField] private string unknownName = "???";
    [SerializeField] private string unknownDesc = "???";

    private readonly List<StageEnemyRow> rows = new();
    private int used;                       // 이번 표시에 실제로 쓴 행 수
    private StageEnemyRow hoverRow;          // 마우스가 올라와 있는 행
    private StageEnemyRow shownRow;          // 툴팁이 떠 있는 행
    private float hoverTimer;
    private bool pointerOverTooltip;
    private bool hidePending;
    private float hideTimer;
    private float zoomScale = 1f;            // StageInfoFollow가 넣어주는 줌 배율(1 = 기준 거리)

    void Awake()
    {
        HideTooltip();
        DisableContainerRaycast();
        CacheTooltipRects();
        SetupTooltipInteraction();
    }

    // 툴팁을 '살아있게' 만드는 배선.
    // 예전엔 툴팁의 모든 Graphic 레이캐스트를 껐다(커서 밑에 걸리면 행의 Enter/Exit가 반복되는 깜빡임 방지).
    // 이제 도감 버튼을 눌러야 하므로 레이캐스트를 살려두고, 대신 '툴팁 위에 있으면 닫지 않기'로 깜빡임을 막는다.
    private void SetupTooltipInteraction()
    {
        if (tooltip == null) return;

        var pointer = tooltip.GetComponent<StageTooltipPointer>();
        if (pointer == null) pointer = tooltip.AddComponent<StageTooltipPointer>();
        pointer.Bind(this);

        if (archiveButton != null)
        {
            archiveButton.onClick.RemoveAllListeners();
            archiveButton.onClick.AddListener(OpenArchiveForShownRow);
        }
    }

    // 툴팁의 도감 버튼. 지금 보고 있는 적 페이지로 도감을 연다.
    private void OpenArchiveForShownRow()
    {
        if (shownRow == null || shownRow.Data == null) return;
        EnemyTable.Data data = shownRow.Data;   // ResetHover가 shownRow를 비우므로 먼저 챈다

        EnemyArchiveManager manager = EnemyArchiveManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("StageInfoView: 씬에 EnemyArchiveManager가 없어 도감을 열 수 없습니다.", this);
            return;
        }
        manager.OpenAt(data);
        ResetHover();   // 도감이 화면을 덮으므로 툴팁은 닫는다
    }

    // 툴팁 위에 커서가 올라왔는지(StageTooltipPointer가 알려준다).
    public void SetPointerOverTooltip(bool over)
    {
        pointerOverTooltip = over;
        if (over) CancelHide();
        else BeginHide();
    }

    // rowContainer는 행을 담기만 하는 껍데기다. 여기에 Image가 붙어 있으면(특히 전체 화면 스트레치)
    // 화면 전체의 클릭을 먹어 IsPointerOverGameObject()가 항상 true가 되고,
    // 그러면 MapCommand→BuildingUiLink 경로가 막혀 타일·바닥 클릭이 전부 죽는다.
    // 자기 Graphic만 끈다 — 자식인 행(row)은 클릭 대상이라 건드리면 안 된다.
    private void DisableContainerRaycast()
    {
        if (rowContainer == null) return;
        if (rowContainer.TryGetComponent(out Graphic g)) g.raycastTarget = false;
    }

    // StageInfoFollow가 패널을 옮긴 직후 호출한다. 같은 오브젝트에 붙은 두 컴포넌트의
    // LateUpdate 순서는 보장되지 않으므로, 옮긴 쪽이 직접 알려줘야 한 프레임도 밀리지 않는다.
    public void RefreshTooltipPosition()
    {
        if (shownRow == null || tooltip == null) return;
        PlaceTooltip(shownRow, false);
    }

    // 줌에 따라 패널이 커지면 툴팁도 같이 커진다. 그런데 tooltipOffset은 화면 픽셀 상수라
    // 그대로 두면 확대할수록 행과의 간격이 상대적으로 좁아져 툴팁이 행 위로 올라탄다.
    // StageInfoFollow가 패널 크기를 바꿀 때마다 같은 배율을 알려준다.
    public void SetZoomScale(float scale) => zoomScale = scale;

    void OnEnable()
    {
        // 이 팝업 문구는 코드가 직접 채우므로 LocalizeText가 붙지 않는다.
        // 언어를 바꿔도 갱신되지 않으니 여기서 직접 구독한다. (EnemyInfo와 같은 이유)
        LocalizeTextManager.OnLanguageChanged += Relocalize;
    }

    void OnDisable()
    {
        LocalizeTextManager.OnLanguageChanged -= Relocalize;
        ResetHover();
    }

    void Update()
    {
        TickHide();                          // 아래 조기 return에 삼켜지지 않게 맨 앞에서 돈다

        if (hoverRow == null) return;
        if (shownRow == hoverRow) return;    // 이미 떠 있음

        hoverTimer += Time.unscaledDeltaTime;
        if (hoverTimer >= hoverDelay) ShowTooltip(hoverRow);
    }

    // 행에서 커서가 벗어나도 바로 닫지 않는다. 툴팁(도감 버튼)까지 가는 동안 빈 공간을 지나므로
    // 여유 시간을 주고, 그 사이 툴팁이나 행에 다시 들어오면 취소된다.
    private void BeginHide()
    {
        if (shownRow == null) return;   // 떠 있는 게 없으면 예약할 것도 없다
        hidePending = true;
        hideTimer = 0f;
    }

    private void CancelHide()
    {
        hidePending = false;
        hideTimer = 0f;
    }

    private void TickHide()
    {
        if (!hidePending) return;
        if (pointerOverTooltip || hoverRow != null) { CancelHide(); return; }

        hideTimer += Time.unscaledDeltaTime;
        if (hideTimer < hideGrace) return;

        ResetHover();   // 안에서 CancelHide + HideTooltip
    }

    // 팝업 패널은 StageInfoFollow가 매 프레임 포탈의 스크린 좌표로 옮긴다.
    // 툴팁은 그 패널의 자식이라 같이 밀려나므로, 화면 밖 보정도 매 프레임 다시 해줘야 한다.
    // (Follow가 없는 구성이나 Follow보다 먼저 도는 경우까지 커버하는 안전망 — 계산은 멱등하다)
    void LateUpdate() => RefreshTooltipPosition();

    // 새 스테이지 정보를 그리기 시작. 떠 있던 행/툴팁을 먼저 정리한다.
    // 팝업 오브젝트는 풀에서 재사용되므로(Despawn해도 자식이 남는다) 매번 반드시 초기화해야 한다.
    public void Begin()
    {
        used = 0;
        ResetHover();
        HideAllRows();
    }

    // 적 한 줄 추가. badgeKey는 "Ui_Add"(증원) / "Ui_Boss"(보스), 일반 웨이브는 null.
    public void AddRow(EnemyTable.Data data, int count, string badgeKey = null)
    {
        if (data == null) return;
        if (rowPrefab == null || rowContainer == null)
        {
            // 이게 비면 팝업이 통째로 빈 채로 뜬다 — 조용히 실패하지 않게 알린다.
            Debug.LogWarning("StageInfoView: rowPrefab / rowContainer 미할당 — 적 목록이 표시되지 않습니다.", this);
            return;
        }

        StageEnemyRow row = GetRow(used++);
        row.gameObject.SetActive(true);
        row.Set(data, count, badgeKey, OnRowHover, OnRowExit, OnRowClick);
    }

    // 팝업을 비운다(밤 전환/다른 칸 클릭 등).
    public void Clear()
    {
        used = 0;
        ResetHover();
        HideAllRows();
    }

    // 행 재사용: 부족할 때만 새로 만들고, 남는 행은 끈다(풀링과 같은 이유로 Destroy 안 함).
    private StageEnemyRow GetRow(int index)
    {
        while (rows.Count <= index)
        {
            StageEnemyRow created = Instantiate(rowPrefab, rowContainer);
            created.name = $"StageEnemyRow_{rows.Count}";
            rows.Add(created);
        }
        return rows[index];
    }

    private void HideAllRows()
    {
        for (int i = used; i < rows.Count; i++)
            if (rows[i] != null) rows[i].gameObject.SetActive(false);
    }

    // 언어가 바뀌면 각 행 문구를 다시 만들고, 떠 있던 툴팁도 새 언어로 교체한다.
    private void Relocalize()
    {
        for (int i = 0; i < used; i++)
            if (rows[i] != null) rows[i].Refresh();
        if (shownRow == null) return;
        FillTooltip(shownRow);
        PlaceTooltip(shownRow, true);   // 언어가 바뀌면 글자 길이가 달라져 위치도 다시 잡아야 한다
    }

    private void OnRowHover(StageEnemyRow row)
    {
        CancelHide();                        // 돌아왔으면 예약된 닫기를 취소(같은 행이어도)
        if (hoverRow == row) return;
        hoverRow = row;
        hoverTimer = 0f;
        if (shownRow != null && shownRow != row) HideTooltip();   // 다른 행으로 옮기면 갈아끼운다
    }

    private void OnRowExit(StageEnemyRow row)
    {
        if (hoverRow != row) return;
        hoverRow = null;
        hoverTimer = 0f;
        BeginHide();                         // 즉시 닫지 않는다 — 도감 버튼까지 갈 시간을 준다
    }

    // 클릭은 hoverDelay 없이 즉시. 같은 행을 다시 누르면 닫는다.
    private void OnRowClick(StageEnemyRow row)
    {
        if (shownRow == row) { HideTooltip(); return; }
        ShowTooltip(row);
    }

    private void ShowTooltip(StageEnemyRow row)
    {
        if (row == null || row.Data == null) return;
        FillTooltip(row);
        if (tooltip != null)
        {
            tooltip.SetActive(true);   // 크기를 재려면 먼저 켜야 한다(레이아웃이 계산됨)
            PlaceTooltip(row, true);
        }
        shownRow = row;
    }

    // 항상 인스펙터 offset이 가리키는 쪽(기본은 행의 왼쪽)에 띄운다.
    // 화면을 벗어나는 만큼만 안으로 밀어 가장자리에 붙인다 — 반대쪽으로 튀지 않는다.
    // 오버레이 캔버스라 position·GetWorldCorners가 전부 화면 픽셀 단위다.
    private void PlaceTooltip(StageEnemyRow row, bool rebuildLayout)
    {
        if (tooltip == null || row == null) return;

        // 글자가 바뀌면 rect가 아직 예전 크기다. 갱신을 먼저 흘려보낸 뒤에 재야 정확하다.
        if (rebuildLayout) Canvas.ForceUpdateCanvases();

        Transform t = tooltip.transform;
        float maxX = Screen.width - screenMargin;
        float maxY = Screen.height - screenMargin;

        t.position = row.transform.position + tooltipOffset * zoomScale;
        if (!MeasureTooltip()) return;   // 잴 게 없으면 그대로 둔다

        // 삐져나간 만큼만 이동. 가장자리에 닿을 때까지는 offset 위치 그대로 따라간다.
        float dx = 0f, dy = 0f;
        if (bLeft < screenMargin) dx = screenMargin - bLeft;
        else if (bRight > maxX) dx = maxX - bRight;
        if (bBottom < screenMargin) dy = screenMargin - bBottom;
        else if (bTop > maxY) dy = maxY - bTop;

        if (dx != 0f || dy != 0f) t.position += new Vector3(dx, dy, 0f);
    }

    private readonly Vector3[] cornerBuf = new Vector3[4];
    private readonly List<RectTransform> tooltipRects = new();
    private float bLeft, bRight, bBottom, bTop;

    // 툴팁 루트는 RectTransform이 아닐 수도 있다(빈 GameObject로 만든 경우).
    // 그래서 루트 rect가 아니라 '보이는 요소들'(Image·TMP)의 rect를 모아 경계를 잡는다.
    // 루트 크기와 실제 내용 크기가 다를 때도 이쪽이 더 정확하다.
    private void CacheTooltipRects()
    {
        tooltipRects.Clear();
        if (tooltip == null) return;
        foreach (var g in tooltip.GetComponentsInChildren<Graphic>(true))
            tooltipRects.Add(g.rectTransform);
    }

    // 지금 위치에서 툴팁이 차지하는 화면 범위. 잴 게 하나도 없으면 false.
    private bool MeasureTooltip()
    {
        bLeft = bBottom = float.MaxValue;
        bRight = bTop = float.MinValue;
        bool any = false;

        for (int i = 0; i < tooltipRects.Count; i++)
        {
            RectTransform r = tooltipRects[i];
            if (r == null || !r.gameObject.activeInHierarchy) continue;

            r.GetWorldCorners(cornerBuf);   // 오버레이 캔버스라 결과가 화면 픽셀
            for (int c = 0; c < 4; c++)
            {
                bLeft = Mathf.Min(bLeft, cornerBuf[c].x);
                bRight = Mathf.Max(bRight, cornerBuf[c].x);
                bBottom = Mathf.Min(bBottom, cornerBuf[c].y);
                bTop = Mathf.Max(bTop, cornerBuf[c].y);
            }
            any = true;
        }
        return any;
    }

    // 이름 + 간단한 설명만. 도감(EnemyInfo)은 페이지/화살표까지 있는 상세 패널이라 여기선 쓰지 않는다.
    private void FillTooltip(StageEnemyRow row)
    {
        var st = DataTableManager.StringTable;
        EnemyTable.Data data = row.Data;
        bool unlocked = EnemyArchiveData.IsUnlocked(data.Name);
        bool masked = maskUnknownEnemy && !unlocked;

        if (tooltipNameText != null)
            tooltipNameText.text = masked ? unknownName : st.Get(data.Name);
        if (tooltipDescText != null)
            tooltipDescText.text = masked ? unknownDesc : st.Get(data.Desc);

        // 도감은 미해금 적을 ???로 띄우고 잠금 팝업까지 낸다(EnemyInfo.ShowLocked).
        // 눌러도 볼 게 없으니 버튼을 숨긴다 — '더 보기'가 덜 보여주는 상황을 막는다.
        if (archiveButton != null) archiveButton.gameObject.SetActive(unlocked);
    }

    private void ResetHover()
    {
        hoverRow = null;
        hoverTimer = 0f;
        CancelHide();
        HideTooltip();
    }

    private void HideTooltip()
    {
        if (tooltip != null) tooltip.SetActive(false);
        shownRow = null;
        // 커서 밑에서 비활성화되면 OnPointerExit가 오지 않는다 → 여기서 직접 내려야 플래그가 안 굳는다.
        pointerOverTooltip = false;
    }
}
