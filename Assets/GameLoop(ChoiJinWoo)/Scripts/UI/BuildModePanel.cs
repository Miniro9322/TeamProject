using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BuildModePanel : MonoBehaviour
{
    [SerializeField] private GameObject heroPanel;
    [SerializeField] private GameObject rosterPanel;
    [SerializeField] private MapView view;
    [SerializeField] private MapGame game;
    [SerializeField] private Key closeKey = Key.Escape;
    private Keyboard keyboard;

    private void Awake()
    {
        heroPanel.SetActive(false);
        rosterPanel.SetActive(false);
        keyboard = Keyboard.current;
    }

    private void Start()
    {
        game.Rule.ChangeToNight += DisablePanels;
        game.EnviromentManager.OnDay += EnablePanel;
    }

    private void OnDestroy()
    {
        game.Rule.ChangeToNight -= DisablePanels;
        game.EnviromentManager.OnDay -= EnablePanel;
    }

    private void Update()
    {
        if (keyboard == null) return;
        if (!keyboard[closeKey].wasPressedThisFrame) return;

        if (!view.IsOff)
        {
            view.ClearMode();
        }
        else if (heroPanel.activeSelf || rosterPanel.activeSelf)
        {
            heroPanel.SetActive(false);
            rosterPanel.SetActive(false);
        }
    }

    private void DisablePanels()
    {
        if (heroPanel.activeSelf)
        {
            heroPanel.SetActive(false);
        }
        gameObject.SetActive(false);
    }

    private void EnablePanel()
    {
        gameObject.SetActive(true);
    }

    public void OnFacilityButton()
    {
        if (heroPanel.activeSelf)
            heroPanel.SetActive(false);
    }

    public void OnHeroButton()
    {
        if (heroPanel.activeSelf)
            heroPanel.SetActive(false);
        else
            heroPanel.SetActive(true);
    }

    public void OnRosterButton()
    {
        if(rosterPanel.activeSelf)
            rosterPanel.SetActive(false);
        else
            rosterPanel.SetActive(true);
    }

    public void OnRemoveButton()
    {
        if (heroPanel.activeSelf)
            heroPanel.SetActive(false);
        view.SetRemove();
    }

    public void OnReplaceButton()
    {
        view.SetReplace();
    }

    public void OnOffButton()
    {
        view.ClearMode();
    }
}
