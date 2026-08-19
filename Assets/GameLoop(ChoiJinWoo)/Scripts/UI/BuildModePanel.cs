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
    private ClickOutsideCloser heroPanelCloser;

    private void Awake()
    {
        heroPanel.SetActive(false);
        rosterPanel.SetActive(false);
        keyboard = Keyboard.current;

        // alsoSelf로 이 패널 전체(빌드모드 버튼들)를 넘겨서, 다른 버튼(예: 로스터)을 눌렀을 때
        // 그 클릭이 "바깥 클릭"으로 잡혀 heroPanel이 먼저 닫혔다가 onClick이 다시 여는 깜빡임을 막는다.
        heroPanelCloser = new ClickOutsideCloser((RectTransform)heroPanel.transform, transform);
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
        if (heroPanel.activeSelf && heroPanelCloser.ClickedOutside())
        {
            heroPanel.SetActive(false);
        }

        if (keyboard == null) return;
        if (!keyboard[closeKey].wasPressedThisFrame) return;

        // 영웅 스킬 시전자 선택/플레이어 스킬 무장이 있으면 그것부터 취소한다(우클릭과 동일한 우선순위).
        if (view.HasArmedOrSelectedSkill)
        {
            view.CancelSkillCasts();
        }
        // 영웅을 집은 상태면 재배치 모드는 유지하고 집은 것만 취소한다.
        else if (view.IsHolding)
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
        {
            heroPanel.SetActive(false);
        }
        else
        {
            heroPanel.SetActive(true);
            heroPanelCloser.MarkOpened();
        }
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
}
