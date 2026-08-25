using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;

public class RegionOverviewPanel : MonoBehaviour, IClosablePanel
{
    [SerializeField] private MapRegistry registry;
    [SerializeField] private RegionDetailPanel detailPanel;
    [SerializeField] private CenterHubPanel hubPanel;
    [SerializeField] private List<RegionNodeView> nodes;
    [SerializeField] private List<RegionFacilitySlots> regions; // 인스펙터에서 지역 오브젝트들을 직접 연결
    [SerializeField] private Button openButton; // 거점 화면을 여는 버튼 - 밤에는 비활성화
    [SerializeField] private GameObject redDot; // 새로 해금된 지역이 있으면 openButton 위에 표시
    [SerializeField] private Key openBaseKey = Key.B; // 거점 화면을 여는 단축키

    private ClickOutsideCloser outsideCloser;

    // RegionDetailPanel이 자기 바깥-클릭 판정에서 지역 노드 버튼만 제외하는 데 쓴다
    // (오버뷰 전체가 아니라 노드들만 - 오버뷰는 화면 전체를 덮고 있어서 전체를 제외하면 바깥 클릭이 아예 안 잡힌다).
    public IReadOnlyList<RegionNodeView> Nodes => nodes;
    public IReadOnlyList<RegionFacilitySlots> Regions => regions;

    private UiPanelStack panelStack;
    private GameManager gameManager;
    private EnviromentManager enviromentManager;
    private bool isNight;

    [Inject]
    private void Construct(UiPanelStack panelStack, GameManager gameManager, EnviromentManager enviromentManager)
    {
        this.panelStack = panelStack;
        this.gameManager = gameManager;
        this.enviromentManager = enviromentManager;

        gameManager.ChangeToNight += OnNight;
        enviromentManager.OnDay += OnDayStart;

        // 열기 단축키는 패널이 닫혀있는 동안(=이 오브젝트가 비활성인 동안) 감지되어야 하는데,
        // 이 오브젝트는 씬에서 처음부터 비활성 상태라 Update()가 전혀 돌지 않는다(레드닷 구독과 동일한 이유).
        // InputSystem.onAfterUpdate는 GameObject 활성 여부와 무관하게 매 업데이트마다 호출되므로 여기서 구독한다.
        InputSystem.onAfterUpdate += CheckOpenHotkey;

        SubscribeModulesDeferred().Forget();
    }

    private void Awake()
    {
        outsideCloser = new ClickOutsideCloser((RectTransform)transform, openButton != null ? openButton.transform : null);
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
        InputSystem.onAfterUpdate -= CheckOpenHotkey;

        foreach (var module in registry.AllModules.Values)
        {
            module.OnStateChanged -= OnAnyModuleUnlocked;
        }
    }

    // 지역이 새로 해금되면(잠김 -> 준비됨) 거점 버튼 위에 레드닷을 띄운다 - 패널을 열면(OpenPanel) 확인한 걸로 치고 끈다.
    private void OnAnyModuleUnlocked(ModuleState state)
    {
        if (state == ModuleState.Preparing && redDot != null)
        {
            redDot.SetActive(true);
        }
    }

    // 밤이 되면 열려있던 거점 화면을 즉시 닫고, 여는 버튼 자체를 꺼버린다
    // (BuildModePanel이 낮/밤 전환에 반응하는 것과 같은 패턴).
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
        panelStack.Push(this);
        BindModules();
        Refresh();
    }

    private void OnDisable()
    {
        panelStack.Remove(this);
        UnbindModules();

        // 오버뷰가 꺼지면 그 아래에서 열려있던 패널들도 다 같이 닫는다.
        // detailPanel.Close()가 buildChoicePanel/buildingPanel까지 정리해주므로 SetActive만 하지 않는다.
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
                node.SetLocked(true); // 대응하는 모듈이 없으면 잠긴 것으로 취급
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

        // 이미 이 지역이 열려있는 채로 같은 노드를 또 누르면 닫는다(토글).
        if (detailPanel.CurrentRegion == region)
        {
            detailPanel.Close();
            return;
        }

        detailPanel.Open(region);
    }

    public void OpenPanel()
    {
        if (isNight) return; // 밤에는 거점 화면을 열 수 없다.
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
        else
            gameObject.SetActive(true);
        outsideCloser.MarkOpened();
        if (redDot != null) redDot.SetActive(false); // 열었으니 확인한 걸로 치고 끈다
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (panelStack.IsTop(this) && outsideCloser.ShouldClose()) Close();
    }

    private void CheckOpenHotkey()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard[openBaseKey].wasPressedThisFrame)
            OpenPanel();
    }
}
