using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;

public class UpgradeUI : MonoBehaviour, IExclusiveUiPanel
{
    [SerializeField] private List<BaseUpgradeData> SystemUpgradeData;
    [SerializeField] private GameObject SystemUpgradeParent;
    [SerializeField] private List<BaseUpgradeData> FacilityUpgradeData;
    [SerializeField] private GameObject FacilityUpgradeParent;
    [SerializeField] private List<BaseUpgradeData> HeroUpgradeData;
    [SerializeField] private GameObject HeroUpgradeParent;
    [SerializeField] private UpgradeInfoUI upgradeInfoPanel;
    [SerializeField] private BaseUpgradeButton buttonPrefab;
    [SerializeField] private TextMeshProUGUI pointsText;
    [SerializeField] private RectTransform openButtonRect;

    private UpgradeState upgradeState;
    private UpgradeStatePlayerPrefsStore fallbackStore; // 스코프 없는 씬에서 폴백으로 만든 경우에만 사용
    private ClickOutsideCloser outsideCloser;
    private PanelReveal panelReveal;
    private readonly Dictionary<BaseUpgradeData, BaseUpgradeButton> nodes = new();

    [Inject]
    private void Construct(UpgradeState upgradeState)
    {
        this.upgradeState = upgradeState;
    }

    private void Awake()
    {
        // 정상 경로: Title.unity 의 TitleLifetimeScope 가 Construct()로 주입.
        // 스코프가 없는 씬(테스트용 TestTitle 등)에서는 자체 인스턴스 + 자체 store 로 폴백한다.
        if (upgradeState == null)
        {
            Debug.LogWarning("UpgradeUI: UpgradeState가 주입되지 않았습니다. 씬에 LifetimeScope가 없어 자체 인스턴스로 폴백합니다.", this);
            upgradeState = new UpgradeState();
            fallbackStore = new UpgradeStatePlayerPrefsStore(upgradeState);
        }

        outsideCloser = new ClickOutsideCloser((RectTransform)transform, openButtonRect);
        panelReveal = GetComponent<PanelReveal>();
    }

    private void OnDestroy()
    {
        fallbackStore?.Dispose();
    }

    private void OnEnable()
    {
        outsideCloser.MarkOpened();
        ExclusiveUiCoordinator.NotifyOpened(this);
        GlobalUiInputSignals.ClickPerformed += HandleCloseCheck;
        GlobalUiInputSignals.EscapePerformed += HandleCloseCheck;
    }

    private void OnDisable()
    {
        ExclusiveUiCoordinator.NotifyClosed(this);
        GlobalUiInputSignals.ClickPerformed -= HandleCloseCheck;
        GlobalUiInputSignals.EscapePerformed -= HandleCloseCheck;
    }

    private void HandleCloseCheck()
    {
        if (outsideCloser.ShouldClose())
        {
            Close();
        }
    }

    // ExclusiveUiCoordinator가 다른 배타 패널이 열렸을 때 이 패널을 닫으라고 부르는 창구.
    public void RequestClose() => Close();

    private void Close()
    {
        if (panelReveal != null) panelReveal.Hide();
        else gameObject.SetActive(false);
    }

    private void Start()
    {
        CreateButton(SystemUpgradeData, SystemUpgradeParent);
        CreateButton(FacilityUpgradeData, FacilityUpgradeParent);
        CreateButton(HeroUpgradeData, HeroUpgradeParent);
        upgradeInfoPanel.ConfirmClicked += OnConfirmUnlock;
        RefreshAll();

        var firstNode = FirstNode();
        if (firstNode != null) OnNodeClicked(firstNode);
    }

    // 패널을 열 때 처음 선택할 노드. 기존엔 Dictionary 순서에 의존한 nodes.First()였다.
    private BaseUpgradeData FirstNode()
    {
        if (SystemUpgradeData.Count > 0) return SystemUpgradeData[0];
        if (FacilityUpgradeData.Count > 0) return FacilityUpgradeData[0];
        if (HeroUpgradeData.Count > 0) return HeroUpgradeData[0];
        return null;
    }

    private void CreateButton(List<BaseUpgradeData> datas, GameObject parents)
    {
        foreach (var data in datas)
        {
            var node = Instantiate(buttonPrefab, parents.transform);
            node.Clicked += OnNodeClicked;
            nodes[data] = node;
        }
    }

    private void OnNodeClicked(BaseUpgradeData data)
    {
        upgradeInfoPanel.Show(data, upgradeState.CanUnlock(data));
    }

    private void OnConfirmUnlock(BaseUpgradeData data)
    {
        if (!upgradeState.CanUnlock(data)) return;
        if (!upgradeState.TrySpendPoints(data.cost)) return;

        upgradeState.Unlock(data.id);
        RefreshAll();
        upgradeInfoPanel.Show(data, false); // 방금 해금했으니 확정 버튼은 다시 비활성화
    }

    private void RefreshAll()
    {
        foreach (var kv in nodes)
            kv.Value.Set(kv.Key, upgradeState.IsUnlocked(kv.Key.id), upgradeState.ArePrerequisitesMet(kv.Key));

        if (pointsText != null)
            pointsText.text = upgradeState.Points.ToString();
    }

    // 리셋 버튼의 OnClick에 연결 — 해금됐던 업그레이드들의 cost를 환불하고 전부 리셋(리스펙)
    public void OnResetButton()
    {
        upgradeState.ResetAll(nodes.Keys);
        RefreshAll();
        upgradeInfoPanel.OnReset(upgradeState.CanUnlock(upgradeInfoPanel.Current));
    }

    public void OnClose()
    {
        Close();
    }
}
