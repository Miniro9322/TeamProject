using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

// 지역 오버뷰 가운데 성 - 총 자원 생산량 확인 + 시민 생성(AddCitizen) 진입점.
// 6개 지역과 달리 ModuleLogic/해금 개념이 없는 고정 허브라 RegionNodeView와 별도로 둔다.
public class CenterHubPanel : MonoBehaviour, IClosablePanel
{
    [SerializeField] private List<ResourceAmountRow> resourceRows; // 자원별 아이콘+수량 한 줄씩, 인스펙터에서 구성
    [SerializeField] private AddCitizen addCitizenPanel;
    [SerializeField] private Button openButton;
    [SerializeField] private Button tradeWoodButton;
    [SerializeField] private Button tradeIronButton;
    [SerializeField] private Button tradeFoodButton;
    [SerializeField] private Button tradeGoldButton;
    [SerializeField] private Button tradeStoneButton;
    [SerializeField] private RegionDetailPanel detailPanel;

    private FacilityManager facilityManager;
    private UiPanelStack panelStack;
    private ClickOutsideCloser outsideCloser;
    private ResourcesManager resourcesManager;

    [Inject]
    private void Construct(FacilityManager facilityManager, UiPanelStack panelStack, ResourcesManager resourcesManager)
    {
        this.facilityManager = facilityManager;
        this.panelStack = panelStack;
        this.resourcesManager = resourcesManager;
    }

    private void Awake()
    {
        if (openButton != null) openButton.onClick.AddListener(Toggle);
        if (addCitizenPanel.gameObject.activeSelf) addCitizenPanel.gameObject.SetActive(false);
        outsideCloser = new ClickOutsideCloser((RectTransform)transform, openButton != null ? openButton.transform : null);
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        panelStack.Push(this);
        outsideCloser.MarkOpened();

        if (tradeFoodButton != null) tradeFoodButton.onClick.AddListener(() => resourcesManager.TradeResource(ProductionType.Food));
        if (tradeIronButton != null) tradeIronButton.onClick.AddListener(() => resourcesManager.TradeResource(ProductionType.Iron));
        if (tradeWoodButton != null) tradeWoodButton.onClick.AddListener(() => resourcesManager.TradeResource(ProductionType.Wood));
        if (tradeGoldButton != null) tradeGoldButton.onClick.AddListener(() => resourcesManager.TradeResource(ProductionType.Gold));
        if (tradeStoneButton != null) tradeStoneButton.onClick.AddListener(() => resourcesManager.TradeResource(ProductionType.Stone));
    }

    private void OnDisable()
    {
        panelStack.Remove(this);

        if (tradeFoodButton != null) tradeFoodButton.onClick.RemoveAllListeners();
        if (tradeIronButton != null) tradeIronButton.onClick.RemoveAllListeners();
        if (tradeWoodButton != null) tradeWoodButton.onClick.RemoveAllListeners();
        if (tradeGoldButton != null) tradeGoldButton.onClick.RemoveAllListeners();
        if (tradeStoneButton != null) tradeStoneButton.onClick.RemoveAllListeners();
    }

    // addCitizenPanel은 하이러키상 자식이 아니라 필드로만 참조되는 별도 패널이라, 열려있는 동안엔
    // 그 안의 클릭을 이 패널의 바깥 클릭으로 오판하지 않도록 판정을 쉰다.
    private void Update()
    {
        if (addCitizenPanel != null && addCitizenPanel.gameObject.activeSelf) return;
        if (outsideCloser.ClickedOutside()) Close();
    }

    public void Toggle()
    {
        if (gameObject.activeSelf)
        {
            Close();
            return;
        }

        gameObject.SetActive(true);
        outsideCloser.MarkOpened();
        if (detailPanel != null) detailPanel.Close();
        Refresh();
    }

    public void Close()
    {
        gameObject.SetActive(false);
        if (addCitizenPanel != null) addCitizenPanel.gameObject.SetActive(false);
    }

    private void Refresh()
    {
        var totals = facilityManager.GetTotalProduction();

        // 생산량이 0인 자원도 표시한다 - 생산 중인 것만 나오면 아예 안 만든 자원인지 0인지 구분이 안 된다.
        foreach (var row in resourceRows)
        {
            int amount = 0;
            foreach (var product in totals)
            {
                if (product.Type == row.type)
                {
                    amount = product.Amount;
                    break;
                }
            }
            if (row.amountText != null) row.amountText.text = $"{amount}/day";
        }
    }

    public void OnCreateCitizen()
    {
        if (addCitizenPanel != null) addCitizenPanel.OpenPanel();
    }
}
