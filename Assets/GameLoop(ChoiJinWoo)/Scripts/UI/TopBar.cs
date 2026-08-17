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
        // 메인 표기는 총 인구 / 실제 수용 한계로 고정한다.
        // 영웅에게 배치된 시민은 여전히 인구에 포함되므로 최대치에서 빼지 않는다.
        citizenText.text = $"{citizenManager.CurrentCitizen}";
        maxCitizenText.text = $"{citizenManager.MaxCitizen}";
        if (idleCitizenText != null) idleCitizenText.text = $"{citizenManager.CanUseCitizen}";

        if(citizenManager.HeroUsedCitizen == 0) heroCitizen.SetActive(false);
        else
        {
            if(!heroCitizen.activeSelf)
                heroCitizen.SetActive(true);
            heroCitizenText.text = $"{citizenManager.HeroUsedCitizen}";
        }
    }
}
