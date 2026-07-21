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
    private ResourcesManager resourcesManager;
    private CitizenManager citizenManager;

    [Inject]
    private void Construct(ResourcesManager resourcesManager, CitizenManager citizenManager)
    {
        this.resourcesManager = resourcesManager;
        this.citizenManager = citizenManager;
    }

    private void Awake()
    {
        resourcesManager.ProductUpdate += UpdateResourcesUI;
        citizenManager.CitizenChanged += UpdateCitizenUi;
    }

    private void OnDestroy()
    {
        resourcesManager.ProductUpdate -= UpdateResourcesUI;
        citizenManager.CitizenChanged -= UpdateCitizenUi;
    }

    private void UpdateResourcesUI()
    {
        woodText.text = $"{resourcesManager.Wood}";
        stoneText.text = $"{resourcesManager.Stone}";
        ironText.text = $"{resourcesManager.Iron}";
        goldText.text = $"{resourcesManager.Gold}";
        foodText.text = $"{resourcesManager.Food}";
    }

    private void UpdateCitizenUi(int max, int cur, int used, int canUse)
    {
        citizenText.text = $"{cur}/{max}/{used}/{canUse}";
    }
}
