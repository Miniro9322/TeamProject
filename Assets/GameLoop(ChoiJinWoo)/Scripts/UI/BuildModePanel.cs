using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BuildModePanel : MonoBehaviour
{
    [SerializeField] private GameObject heroPanel;
    //[SerializeField] private GameObject upgradePanel;
    [SerializeField] private GameObject classUpgradePanel;
    [SerializeField] private GameObject cheatPanel;
    [SerializeField] private HeroArchiveButton heroArchiveButton;
    [SerializeField] private GameObject heroInventory;
    [SerializeField] private HeroCreateAmountController heroCreateAmountPanel;
    [SerializeField] private MapView view;
    [SerializeField] private MapGame game;
    [SerializeField] private BuildPanelSlide panelSlide;
    [SerializeField] private Key closeKey = Key.Escape;
    [SerializeField] private Key upgradeKey = Key.U;
    [SerializeField] private Key replaceKey = Key.R;
    [SerializeField] private Key removeKey = Key.E;
    [SerializeField] private Key inventoryKey = Key.I;
    private Keyboard keyboard;
    private ClickOutsideCloser heroPanelCloser;
    private ClickOutsideCloser inventoryCloser;
    private ClickOutsideCloser classUpgradeCloser;
    private bool _reopenInventoryOnOff;
    private GameObject lastSelectedGameObject; // 재배치/회수 모드 중 다른 버튼 클릭 감지용

    private void OnEnable()
    {
        view.OnOffMode += HandleMapOff;
    }

    private void OnDisable()
    {
        view.OnOffMode -= HandleMapOff;
    }

    // 재배치/제거 모드가 끝나 Off로 돌아오면(버튼/단축키/ESC 등 어떤 경로든) 그때만 인벤토리를 다시 연다.
    private void HandleMapOff()
    {
        if (!_reopenInventoryOnOff) return;
        _reopenInventoryOnOff = false;
        OpenInventory();
    }

    private void Awake()
    {
        heroPanel.SetActive(false);
        heroInventory.SetActive(false);
        classUpgradePanel.SetActive(false);

        // classUpgradePanel/heroInventory는 전용 스크립트가 없는 순수 GameObject라, OnEnable/OnDisable로
        // ExclusiveUiCoordinator에 알려줄 컴포넌트를 여기서 붙여준다(씬/프리팹을 직접 안 건드리기 위해).
        if (classUpgradePanel.GetComponent<ExclusivePanelPresence>() == null)
            classUpgradePanel.AddComponent<ExclusivePanelPresence>();
        if (heroInventory.GetComponent<ExclusivePanelPresence>() == null)
            heroInventory.AddComponent<ExclusivePanelPresence>();

        keyboard = Keyboard.current;

        // alsoSelf로 이 패널 전체(빌드모드 버튼들)를 넘겨서, 다른 버튼(예: 로스터)을 눌렀을 때
        // 그 클릭이 "바깥 클릭"으로 잡혀 heroPanel이 먼저 닫혔다가 onClick이 다시 여는 깜빡임을 막는다.
        heroPanelCloser = new ClickOutsideCloser((RectTransform)heroPanel.transform, transform,
            heroCreateAmountPanel != null ? (RectTransform)heroCreateAmountPanel.transform : null);
        inventoryCloser = new ClickOutsideCloser((RectTransform)heroInventory.transform, transform, (RectTransform)heroPanel.transform);
        classUpgradeCloser = new ClickOutsideCloser((RectTransform)classUpgradePanel.transform, transform, (RectTransform)classUpgradePanel.transform);
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

    // ESC로 메뉴를 열지 말지 판단할 때 쓴다(UiManager) - 여기서 취소/닫을 게 있으면 ESC는
    // 메뉴를 여는 대신 그것부터 처리해야 하므로, Update()의 ESC 분기와 조건을 그대로 맞춘다.
    public bool HasEscapeCancelable =>
        view.HasArmedOrSelectedSkill
        || view.IsHolding
        || !view.IsOff
        || heroPanel.activeSelf
        || heroInventory.activeSelf
        || classUpgradePanel.activeSelf
        || (cheatPanel != null && cheatPanel.activeSelf)
        || (heroArchiveButton != null && heroArchiveButton.IsOpen)
        || (heroCreateAmountPanel != null && heroCreateAmountPanel.gameObject.activeInHierarchy);

    private void Update()
    {
        if (heroPanel.activeSelf && heroPanelCloser.ClickedOutside())
        {
            heroPanel.SetActive(false);
        }
        if (classUpgradePanel.activeSelf && classUpgradeCloser.ClickedOutside())
        {
            classUpgradePanel.SetActive(false);
        }
        if (heroInventory.activeSelf && view.IsOff && inventoryCloser.ClickedOutside())
        {
            heroInventory.SetActive(false);
        }

        // 재배치/제거 모드 중 다른 버튼을 누르면 그 모드를 빠져나온다. 맵 타일 클릭은 uGUI Selectable이
        // 아니라 currentSelectedGameObject를 바꾸지 않으므로 여기 걸리지 않고, Replace/Remove 버튼
        // 자체는 ReplaceHeldLink/RemoveHeldLink로 식별해 제외한다(그 두 버튼은 OnReplaceButton/
        // OnRemoveButton이 이미 토글로 직접 처리함).
        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (selected != lastSelectedGameObject)
        {
            lastSelectedGameObject = selected;
            if (selected != null && (view.IsReplacing || view.IsRemoving))
            {
                Button clickedButton = selected.GetComponent<Button>();
                if (clickedButton != null
                    && clickedButton.GetComponent<ReplaceHeldLink>() == null
                    && clickedButton.GetComponent<RemoveHeldLink>() == null)
                {
                    view.ClearMode();
                }
            }
        }

        if (keyboard == null) return;

        if (keyboard[upgradeKey].wasPressedThisFrame && !TutorialInputGate.BlockHotkeys)
            OnClassUpgradeButton();

        if (keyboard[replaceKey].wasPressedThisFrame && !TutorialInputGate.BlockHotkeys)
            OnReplaceButton();

        if (keyboard[removeKey].wasPressedThisFrame && !TutorialInputGate.BlockHotkeys)
            OnRemoveButton();

        if (keyboard[inventoryKey].wasPressedThisFrame && !TutorialInputGate.BlockHotkeys)
            OnInventoryButton();

        if (!keyboard[closeKey].wasPressedThisFrame) return;
        if (TutorialInputGate.BlockEscapeClose) return;

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
        else if (heroPanel.activeSelf || heroInventory.activeSelf || classUpgradePanel.activeSelf
            || (cheatPanel != null && cheatPanel.activeSelf)
            || (heroArchiveButton != null && heroArchiveButton.IsOpen))
        {
            heroPanel.SetActive(false);
            heroInventory.SetActive(false);
            classUpgradePanel.SetActive(false);
            if (cheatPanel != null) cheatPanel.SetActive(false);
            heroArchiveButton?.Close();
        }
    }

    // 열린 하위 패널을 닫고 빌드 패널의 퇴장 연출을 시작한다.
    private void DisablePanels()
    {
        if (heroPanel.activeSelf)
        {
            heroPanel.SetActive(false);
        }
        if (classUpgradePanel.activeSelf)
        {
            classUpgradePanel.SetActive(false);
        }
        if (heroInventory.activeSelf)
        {
            heroInventory.SetActive(false);
        }
        // 이 GameObject가 바로 아래에서 꺼지면 Update()의 ESC 우선순위 체인도 같이 멈춘다 - 영웅
        // 도감은 자기 ESC 처리를 그 체인에만 맡겨두므로(HeroArchiveButton.cs 참고), 열려있는 채로
        // 밤을 맞으면 낮이 될 때까지 ESC/바깥클릭/P키 그 무엇으로도 못 닫는 상태가 된다. 다른
        // 패널들처럼 여기서 미리 닫아준다.
        heroArchiveButton?.Close();
        panelSlide.Close();
    }

    // 낮 전환이 끝난 빌드 패널의 등장 연출을 시작한다.
    private void EnablePanel()
    {
        panelSlide.Open();
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
            if (heroInventory.activeSelf) heroInventory.SetActive(false);
            if (classUpgradePanel.activeSelf) classUpgradePanel.SetActive(false);
        }
    }

    public void OnRemoveButton()
    {
        if (view.IsRemoving)
        {
            view.ClearMode();
        }
        else
        {
            CloseInventoryForMode();
            view.SetRemove();
        }
    }

    public void OnReplaceButton()
    {
        if (view.IsReplacing)
        {
            view.ClearMode();
        }
        else
        {
            CloseInventoryForMode();
            view.SetReplace();
        }
    }

    // 재배치/제거 모드로 들어가는 동안 인벤토리가 열려 있었다면 닫아두고, 모드가 끝나면 다시 연다.
    private void CloseInventoryForMode()
    {
        if (!heroInventory.activeSelf) return;
        heroInventory.SetActive(false);
        _reopenInventoryOnOff = true;
    }

    public void OnInventoryButton()
    {
        if (heroInventory.activeSelf)
        {
            heroInventory.SetActive(false);
        }
        else
        {
            OpenInventory();
        }
    }

    public void OpenInventory()
    {
        if (heroInventory.activeSelf) return;

        heroInventory.SetActive(true);
        inventoryCloser.MarkOpened();
        if (classUpgradePanel.activeSelf) classUpgradePanel.SetActive(false);
        if (heroPanel.activeSelf) heroPanel.SetActive(false);
    }

    public void OnClassUpgradeButton()
    {
        // 튜토리얼이 이 버튼을 스포트라이트로 짚어 "언급"만 하는 중일 땐 실제로 눌려서 패널이
        // 열리면 안 된다 - TutorialInputGate.BlockHeroUpgradeOpen 참고. 이 버튼은 인스펙터에서
        // 곧바로 이 메서드에 연결돼 HeroTierUpgradeMenu.Toggle()을 거치지 않으므로 여기서도 따로 막는다.
        if (TutorialInputGate.BlockHeroUpgradeOpen) return;

        if (classUpgradePanel.activeSelf)
        {
            classUpgradePanel.SetActive(false);
        }
        else
        {
            classUpgradePanel.SetActive(true);
            classUpgradeCloser.MarkOpened();
            if (heroInventory.activeSelf) heroInventory.SetActive(false);
            if (heroPanel.activeSelf) heroPanel.SetActive(false);
        }
    }

    public void OnOffButton()
    {
        view.ClearMode();
    }
}
