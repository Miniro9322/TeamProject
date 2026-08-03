using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

// 빈 슬롯에 지을 건물 종류를 고르는 팝업. 생산 시설/집은 더 이상 맵 팔레트(PlacePalette)를 안 거치고
// 여기서 직접 BuildableFacility 목록(ProductionValue/HouseConfig 참조)으로 관리한다.
public class FacilityBuildChoicePanel : MonoBehaviour, IClosablePanel
{
    [SerializeField] private List<BuildableFacility> options;
    [SerializeField] private List<BuildOptionView> optionViews; // options와 인덱스가 대응
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private Image infoIcon;
    [SerializeField] private TextMeshProUGUI infoText;

    private BaseConstructor constructor;
    private ResourcesManager resourcesManager;
    private UpgradeState upgradeState;
    private ProductionEconomyConfig economyConfig;
    private UiPanelStack panelStack;
    private RegionFacilitySlots region;
    private int slotIndex;
    private BuildableFacility currentOption;
    private ClickOutsideCloser outsideCloser;
    private ClickOutsideCloser infoOutsideCloser;

    [Inject]
    private void Construct(BaseConstructor constructor, ResourcesManager resourcesManager, UpgradeState upgradeState, ProductionEconomyConfig economyConfig, UiPanelStack panelStack)
    {
        this.constructor = constructor;
        this.resourcesManager = resourcesManager;
        this.upgradeState = upgradeState;
        this.economyConfig = economyConfig;
        this.panelStack = panelStack;
    }

    private void Awake()
    {
        // 인스펙터에서 버튼마다 고정 인덱스를 손으로 넣으면 실수하기 쉬워 코드로 연결한다.
        for (int i = 0; i < optionViews.Count; i++)
        {
            optionViews[i].SetIndex(i);
            optionViews[i].BindClick(OnOption);
        }

        outsideCloser = new ClickOutsideCloser((RectTransform)transform);
        infoOutsideCloser = new ClickOutsideCloser((RectTransform)infoPanel.transform);
    }

    private void OnEnable()
    {
        panelStack.Push(this);
        resourcesManager.ProductUpdate += RefreshButtons;
        infoPanel.SetActive(false);
        RefreshButtons();
        outsideCloser.MarkOpened();
    }

    private void OnDisable()
    {
        panelStack.Remove(this);
        resourcesManager.ProductUpdate -= RefreshButtons;
    }

    // 정보 패널 바깥(그러나 이 패널 안)을 클릭하면 정보 패널만 닫고, 이 패널 바깥을 클릭하면 이 패널 전체를 닫는다.
    private void Update()
    {
        if (infoPanel.activeSelf && infoOutsideCloser.ClickedOutside())
        {
            infoPanel.SetActive(false);
            return;
        }

        if (outsideCloser.ClickedOutside()) Close();
    }

    public void Open(RegionFacilitySlots target, int index)
    {
        region = target;
        slotIndex = index;
        gameObject.SetActive(true);
        outsideCloser.MarkOpened();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void RefreshButtons()
    {
        for (int i = 0; i < optionViews.Count && i < options.Count; i++)
        {
            optionViews[i].SetOption(options[i].icon, options[i].DisplayName);
            optionViews[i].SetInteractable(constructor.CanBuild(options[i]));
        }
    }

    public void OnOption(int index)
    {
        if (index < 0 || index >= options.Count) return;

        currentOption = options[index];
        infoIcon.sprite = currentOption.icon;
        var sb = new StringBuilder();

        if (currentOption.kind == OccupantKind.Resource && currentOption.facilityValue != null)
        {
            var value = currentOption.facilityValue;
            var cost = ProductionFacility.PreviewConstructCost(value, economyConfig, upgradeState);
            sb.Append($"{value.FacilityName}\n{value.FacilityInfo}\n생산 자원: {value.Type}\n건설 소모 자원\n");
            foreach (var c in cost)
            {
                sb.Append($"{c.Type}: {-c.Amount} ");
            }
        }
        else if (currentOption.houseConfig != null)
        {
            var config = currentOption.houseConfig;
            sb.Append($"{config.HouseName}\n{config.HouseInfo}\n건설 소모 자원\n");
            foreach (var c in config.Resources)
            {
                sb.Append($"{c.Type}: {-c.Amount} ");
            }
        }
        infoText.text = sb.ToString().Trim();
        infoPanel.SetActive(true);
        infoOutsideCloser.MarkOpened();
    }

    public void OnBuild()
    {
        if (currentOption == null || region == null) return;

        if (constructor.TryBuild(currentOption, region, slotIndex, out _))
        {
            infoPanel.SetActive(false);
            Close();
        }
    }
}
