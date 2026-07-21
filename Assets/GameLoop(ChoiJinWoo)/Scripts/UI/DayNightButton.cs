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
    }

    private void OnButton()
    {
        //enviromentManager.ToggleDayNight();
        gameManager.OnNight();
    }

    private void OnDestroy()
    {
        button.onClick.RemoveAllListeners();
    }
}
