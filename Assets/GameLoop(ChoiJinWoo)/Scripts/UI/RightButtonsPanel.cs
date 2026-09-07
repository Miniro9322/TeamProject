using UnityEngine;

public class RightButtonsPanel : MonoBehaviour
{
    [SerializeField] private MapGame game;

    private void Start()
    {
        game.Rule.ChangeToNight += Hide;
        game.EnviromentManager.OnDay += Show;
    }

    private void OnDestroy()
    {
        game.Rule.ChangeToNight -= Hide;
        game.EnviromentManager.OnDay -= Show;
    }

    private void Hide() => gameObject.SetActive(false);
    private void Show() => gameObject.SetActive(true);
}
