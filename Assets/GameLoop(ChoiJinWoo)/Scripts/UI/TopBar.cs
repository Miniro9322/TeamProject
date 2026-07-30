using TMPro;
using UnityEngine;
using VContainer;

public class TopBar : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI woodText;
    [SerializeField] private TextMeshProUGUI stoneText;
    [SerializeField] private TextMeshProUGUI ironText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI foodText;
    [SerializeField] private TextMeshProUGUI citizenText;
    [SerializeField] private TextMeshProUGUI LifeText;
    private ResourcesManager resourcesManager;
    private CitizenManager citizenManager;
    private GameManager gameManager;

    [Inject]
    private void Construct(ResourcesManager resourcesManager, CitizenManager citizenManager, GameManager gameManager)
    {
        this.resourcesManager = resourcesManager;
        this.citizenManager = citizenManager;
        this.gameManager = gameManager;
    }

    private void Awake()
    {
        resourcesManager.ProductUpdate += UpdateResourcesUI;
        citizenManager.CitizenChanged += UpdateCitizenUi;
        gameManager.HpChanged += UpdateLife;
        UpdateResourcesUI();
        UpdateCitizenUi();
        UpdateLife();
    }

    private void OnDestroy()
    {
        resourcesManager.ProductUpdate -= UpdateResourcesUI;
        citizenManager.CitizenChanged -= UpdateCitizenUi;
        gameManager.HpChanged -= UpdateLife;
    }

    private void UpdateResourcesUI()
    {
        woodText.text = $"{resourcesManager.Wood}";
        stoneText.text = $"{resourcesManager.Stone}";
        ironText.text = $"{resourcesManager.Iron}";
        goldText.text = $"{resourcesManager.Gold}";
        foodText.text = $"{resourcesManager.Food}";
    }

    private void UpdateLife()
    {
        if (LifeText == null) return;
        LifeText.text = $"{gameManager.Hp}";
    }

    private void UpdateCitizenUi()
    {
        citizenText.text = $"현재 시민 수: {citizenManager.CurrentCitizen}/최대 시민 수: {citizenManager.MaxCitizen}\n배치 가능 시민: {citizenManager.CanUseCitizen}/배치한 시민: {citizenManager.UsedCitizen}";
    }
}
