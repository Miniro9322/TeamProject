using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;

public class RegionOverviewPanel : MonoBehaviour, IClosablePanel, IExclusiveUiPanel, IPersistentAcrossExclusivePanels
{
    [SerializeField] private MapRegistry registry;
    [SerializeField] private RegionDetailPanel detailPanel;
    [SerializeField] private CenterHubPanel hubPanel;
    [SerializeField] private List<RegionNodeView> nodes;
    [SerializeField] private List<RegionFacilitySlots> regions;
    [SerializeField] private Button openButton;
    [SerializeField] private GameObject redDot;
    [SerializeField] private Key openBaseKey = Key.B;

    private ClickOutsideCloser outsideCloser;

    public IReadOnlyList<RegionNodeView> Nodes => nodes;
    public IReadOnlyList<RegionFacilitySlots> Regions => regions;

    private UiPanelStack panelStack;
    private GameManager gameManager;
    private EnviromentManager enviromentManager;
    private InputAction openHotkeyAction;
    private bool isNight;
    private bool regionUnlockNoticeSeen = true;

    public bool RegionUnlockNoticeSeen => regionUnlockNoticeSeen;

    [Inject]
    private void Construct(UiPanelStack panelStack, GameManager gameManager, EnviromentManager enviromentManager)
    {
        this.panelStack = panelStack;
        this.gameManager = gameManager;
        this.enviromentManager = enviromentManager;

        gameManager.ChangeToNight += OnNight;
        enviromentManager.OnDay += OnDayStart;

        openHotkeyAction = new InputAction("OpenRegionOverview", binding: Keyboard.current[openBaseKey].path);
        openHotkeyAction.performed += OnOpenHotkeyPerformed;
        openHotkeyAction.Enable();

        outsideCloser = new ClickOutsideCloser((RectTransform)transform, openButton != null ? openButton.transform : null);

        SubscribeModulesDeferred().Forget();
    }

    private async UniTaskVoid SubscribeModulesDeferred()
    {
        await UniTask.Yield();
        foreach (var module in registry.AllModules.Values)
        {
            module.OnStateChanged += OnAnyModuleUnlocked;
        }
    }

    private void OnDestroy()
    {
        gameManager.ChangeToNight -= OnNight;
        enviromentManager.OnDay -= OnDayStart;

        openHotkeyAction.performed -= OnOpenHotkeyPerformed;
        openHotkeyAction.Disable();
        openHotkeyAction.Dispose();

        foreach (var module in registry.AllModules.Values)
        {
            module.OnStateChanged -= OnAnyModuleUnlocked;
        }
    }

    private void OnAnyModuleUnlocked(ModuleState state)
    {
        if (state == ModuleState.Preparing && redDot != null)
        {
            regionUnlockNoticeSeen = false;
            redDot.SetActive(true);
        }
    }

    public void RestoreNoticeSeen(bool seen)
    {
        regionUnlockNoticeSeen = seen;
        if (redDot != null) redDot.SetActive(!seen);
    }

    private void OnNight()
    {
        isNight = true;
        if (openButton != null) openButton.gameObject.SetActive(false);
        Close();
    }

    private void OnDayStart()
    {
        isNight = false;
        if (openButton != null) openButton.gameObject.SetActive(true);
    }

    private void OnEnable()
    {
        ExclusiveUiCoordinator.NotifyOpened(this);
        regionUnlockNoticeSeen = true;
        if (redDot != null && redDot.activeSelf) redDot.SetActive(false); // 열었으니 확인한 걸로 치고 끈다
        panelStack.Push(this);
        BindModules();
        Refresh();
        GlobalUiInputSignals.ClickPerformed += HandleOutsideClick;
        GlobalUiInputSignals.EscapePerformedLow += HandleEscapeClose;
    }

    private void OnDisable()
    {
        ExclusiveUiCoordinator.NotifyClosed(this);
        panelStack.Remove(this);
        UnbindModules();
        GlobalUiInputSignals.ClickPerformed -= HandleOutsideClick;
        GlobalUiInputSignals.EscapePerformedLow -= HandleEscapeClose;

        detailPanel.Close();
        if (hubPanel != null) hubPanel.Close();
    }

    private void BindModules()
    {
        foreach (var module in registry.AllModules.Values)
        {
            module.OnStateChanged += OnModuleState;
        }
    }

    private void UnbindModules()
    {
        foreach (var module in registry.AllModules.Values)
        {
            module.OnStateChanged -= OnModuleState;
        }
    }

    private void OnModuleState(ModuleState state)
    {
        Refresh();
    }

    private RegionFacilitySlots FindRegion(int moduleId)
    {
        foreach (var region in regions)
        {
            if (region.ModuleId == moduleId) return region;
        }
        return null;
    }

    private void Refresh()
    {
        foreach (var node in nodes)
        {
            if (!registry.TryGetModuleLogic(node.ModuleId, out var module))
            {
                node.SetLocked(true);
                continue;
            }

            node.SetLocked(!module.IsUnlocked);
        }
    }

    public void OnNodeClicked(RegionNodeView node)
    {
        if (!registry.TryGetModuleLogic(node.ModuleId, out var module) || !module.IsUnlocked) return;

        var region = FindRegion(node.ModuleId);
        if (region == null) return;

        if (detailPanel.CurrentRegion == region)
        {
            detailPanel.Close();
            return;
        }

        if (hubPanel != null) hubPanel.Close();
        detailPanel.Open(region);
    }

    public void OpenPanel()
    {
        if (isNight) return;

        if (TutorialInputGate.BlockPanelOpen) return;

        if (gameObject.activeSelf) return;

        outsideCloser.MarkOpened();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void RequestClose() => Close();

    // 바깥 클릭: 지역 스택 맨 위(상세/허브 패널이 위에 없을 때)일 때만 닫는다.
    private void HandleOutsideClick()
    {
        if (panelStack.IsTop(this) && outsideCloser.ClickedOutside()) Close();
    }

    // Esc: 이 패널은 모달 패널(메뉴/가이드 등)보다 우선순위가 낮은 EscapePerformedLow로 받는다.
    // 위에 뜬 모달이 이번 Esc를 이미 소비했다면 이 단계는 호출되지 않으므로, 예전처럼
    // "다른 배타 패널이 떠 있었나"를 LateUpdate로 폴링(hadOtherExclusivePanelLastFrame)할 필요가 없다.
    private void HandleEscapeClose()
    {
        if (TutorialInputGate.BlockEscapeClose) return;
        if (!panelStack.IsTop(this)) return;
        Close();
        GlobalUiInputSignals.ConsumeEscape();
    }

    private void OnOpenHotkeyPerformed(InputAction.CallbackContext context)
    {
        if (TutorialInputGate.BlockHotkeys) return;
        if (isNight) return;

        if (gameObject.activeSelf)
            gameObject.SetActive(false);
        else
            gameObject.SetActive(true);
    }
}
