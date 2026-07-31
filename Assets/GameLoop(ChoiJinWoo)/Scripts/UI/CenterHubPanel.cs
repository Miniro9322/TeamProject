using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

// 지역 오버뷰 가운데 성 - 총 자원 생산량 확인 + 시민 생성(AddCitizen) 진입점.
// 6개 지역과 달리 ModuleLogic/해금 개념이 없는 고정 허브라 RegionNodeView와 별도로 둔다.
public class CenterHubPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI productionText;
    [SerializeField] private AddCitizen addCitizenPanel;
    [SerializeField] private Button openButton;

    private FacilityManager facilityManager;

    [Inject]
    private void Construct(FacilityManager facilityManager)
    {
        this.facilityManager = facilityManager;
    }

    private void Awake()
    {
        if (openButton != null) openButton.onClick.AddListener(Toggle);
    }

    public void Toggle()
    {
        bool next = !gameObject.activeSelf;
        gameObject.SetActive(next);
        if (next) Refresh();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private void Refresh()
    {
        var sb = new StringBuilder();
        sb.Append("일일 총 생산량\n");
        foreach (var product in facilityManager.GetTotalProduction())
        {
            sb.Append($"{product.Type}: {product.Amount}/day\n");
        }
        productionText.text = sb.ToString().TrimEnd();
    }

    public void OnCreateCitizen()
    {
        if (addCitizenPanel != null) addCitizenPanel.OpenPanel();
    }
}
