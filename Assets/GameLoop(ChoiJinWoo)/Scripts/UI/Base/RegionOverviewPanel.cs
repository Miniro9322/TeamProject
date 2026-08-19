using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;

// 기반시설 UI 최상단 화면 - 지역 다이아몬드 오버뷰(§허브 + 6지역).
// 각 노드는 ModuleLogic.IsUnlocked를 그대로 반영한다 - 해금 자체는 기존 ExpandEvent 흐름을 그대로 탄다.
// RegionFacilitySlots는 모듈 프리팹에 붙어있지 않고(충돌 방지) moduleId로만 엮이므로, regions 리스트에서
// 직접 찾는다(GetComponent 아님).
// ESC 키는 여기서만 감지해서 UiPanelStack.CloseTop()으로 넘긴다 - 거점 UI 하위 패널들은 전부
// OnEnable/OnDisable로 스택에 오르내리므로, 제일 나중에 연 패널부터 하나씩 닫힌다.
public class RegionOverviewPanel : MonoBehaviour, IClosablePanel
{
    [SerializeField] private MapRegistry registry;
    [SerializeField] private RegionDetailPanel detailPanel;
    [SerializeField] private CenterHubPanel hubPanel;
    [SerializeField] private List<RegionNodeView> nodes;
    [SerializeField] private List<RegionFacilitySlots> regions; // 인스펙터에서 지역 오브젝트들을 직접 연결
    [SerializeField] private Button openButton; // 거점 화면을 여는 버튼 - 밤에는 비활성화
    [SerializeField] private GameObject redDot; // 새로 해금된 지역이 있으면 openButton 위에 표시

    // RegionDetailPanel이 자기 바깥-클릭 판정에서 지역 노드 버튼만 제외하는 데 쓴다
    // (오버뷰 전체가 아니라 노드들만 - 오버뷰는 화면 전체를 덮고 있어서 전체를 제외하면 바깥 클릭이 아예 안 잡힌다).
    public IReadOnlyList<RegionNodeView> Nodes => nodes;

    // 지역 슬롯 목록을 그대로 열거한다 (세이브·로드 전용 통로)
    public IReadOnlyList<RegionFacilitySlots> Regions => regions;

    private UiPanelStack panelStack;
    private GameManager gameManager;
    private EnviromentManager enviromentManager;
    private bool isNight;

    // 낮/밤 구독은 Start가 아니라 여기서 한다 - 이 오브젝트는 씬에서 처음부터 비활성 상태라
    // (버튼을 눌러야 처음 활성화됨) Start/Awake는 한 번도 안 켜지면 영영 안 불린다. [Inject]는
    // 활성 여부와 무관하게 VContainer가 실행해주므로, 버튼을 한 번도 안 눌러도 구독이 걸린다.
    [Inject]
    private void Construct(UiPanelStack panelStack, GameManager gameManager, EnviromentManager enviromentManager)
    {
        this.panelStack = panelStack;
        this.gameManager = gameManager;
        this.enviromentManager = enviromentManager;

        gameManager.ChangeToNight += OnNight;
        enviromentManager.OnDay += OnDayStart;

        // 레드닷 감지는 패널이 닫혀있어도 계속 돌아야 해서(닫힌 버튼 위에 띄우는 용도) OnEnable이 아니라
        // 여기서 항상 구독해둔다. Refresh용 BindModules()/UnbindModules()와는 별개다.
        // 한 프레임 미뤄서 구독한다 - Construct는 MapRegistry.Awake()보다 먼저 돌 수도 있어서,
        // 그 시점에 바로 registry.AllModules를 돌면 아직 비어있어 구독이 하나도 안 걸릴 수 있다.
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

        gameObject.SetActive(true);
        if (redDot != null) redDot.SetActive(false); // 열었으니 확인한 걸로 치고 끈다
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            panelStack.CloseTop();
        }
    }
}
