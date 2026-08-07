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

        // 영웅을 집은 상태면 재배치 모드는 유지하고 집은 것만 취소한다.
        if (view.IsHolding)
        {
            view.CancelHold();
        }
        else if (!view.IsOff)
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
        if (view.IsRemoving)
            view.ClearMode();
        else
            view.SetRemove();
    }

    public void OnReplaceButton()
    {
        if (view.IsReplacing)
            view.ClearMode();
        else
            view.SetReplace();
    }

    public void OnOffButton()
    {
        heroPanel.SetActive(false);
        view.ClearMode();
    }
}
