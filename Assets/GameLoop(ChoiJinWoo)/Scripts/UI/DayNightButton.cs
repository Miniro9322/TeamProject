using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class DayNightButton : MonoBehaviour
{
    [SerializeField] private Button button;
    private GameManager gameManager;
    private EnviromentManager enviromentManager;

    [Inject]
    private void Construct(GameManager gameManager, EnviromentManager enviromentManager)
    {
        this.gameManager = gameManager;
        this.enviromentManager = enviromentManager;
    }

    private void Start()
    {
        button.onClick.AddListener(OnButton);
        enviromentManager.OnDay += EnableButton;
    }

    private void OnButton()
    {
        gameManager.OnNight();
        gameObject.SetActive(false);
    }

    private void EnableButton()
    {
        gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        button.onClick.RemoveAllListeners();
        gameManager.ChangeToDay -= EnableButton;
    }
}
