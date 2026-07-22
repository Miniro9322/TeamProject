using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class DayNightButton : MonoBehaviour
{
    [SerializeField] private Button button;
    private EnviromentManager enviromentManager;
    private GameManager gameManager;

    [Inject]
    private void Construct(EnviromentManager enviromentManager, GameManager gameManager)
    {
        this.enviromentManager = enviromentManager;
        this.gameManager = gameManager;
    }

    private void Start()
    {
        button.onClick.AddListener(OnButton);
        gameManager.ChangeToDay += EnableButton;
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
