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
    [SerializeField] private TextMeshProUGUI maxCitizenText;
    [SerializeField] private TextMeshProUGUI idleCitizenText;
    [SerializeField] private TextMeshProUGUI heroCitizenText;
    [SerializeField] private TextMeshProUGUI LifeText;
    [SerializeField] private GameObject heroCitizen;
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
        if (citizenText != null) citizenText.text = $"{citizenManager.CurrentCitizen}";
        if (maxCitizenText != null) maxCitizenText.text = $"{citizenManager.MaxCitizen}";
        if (idleCitizenText != null) idleCitizenText.text = $"{citizenManager.CanUseCitizen}";
        if (heroCitizenText != null) heroCitizenText.text = $"{citizenManager.HeroUsedCitizen}";
    }
}
