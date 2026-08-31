using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BuildModePanel : MonoBehaviour
{
    [SerializeField] private GameObject heroPanel;
    //[SerializeField] private GameObject upgradePanel;
    [SerializeField] private GameObject classUpgradePanel;
    //[SerializeField] private GameObject cheatPanel;
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
    [SerializeField] private Key createHeroKey = Key.C;
    private Keyboard keyboard;
    private ClickOutsideCloser heroPanelCloser;
    private ClickOutsideCloser inventoryCloser;
    private ClickOutsideCloser classUpgradeCloser;
    private GameObject lastSelectedGameObject; // 재배치/회수 모드 중 다른 버튼 클릭 감지용

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

    // 열린 하위 패널을 닫고 빌드 패널의 퇴장 연출을 시작한다.
    private void DisablePanels()
    {
        if (heroPanel.activeSelf) SetPanelOpen(heroPanel, false);
        if (classUpgradePanel.activeSelf) SetPanelOpen(classUpgradePanel, false);
        if (heroInventory.activeSelf) heroInventory.SetActive(false);
        heroArchiveButton?.Close();
        panelSlide.Close();
    }

    // heroPanel/classUpgradePanel에 PanelReveal이 붙어 있으면 그 스케일 연출로 열고 닫는다(버튼의
    // UIButtonHeld.Toggle()과 같은 규칙). 여기서 안 이러면 클릭과 단축키가 서로 다른 방식으로 여닫게
    // 되고, PanelReveal.Hide()가 스케일을 0으로 남겨둔 채 끝내는데 이후 SetActive(true)만 부르면
    // 오브젝트는 켜지지만 스케일이 0인 채로 남아 안 보이게 된다.
    private static void SetPanelOpen(GameObject panel, bool open)
    {
        PanelReveal reveal = panel.GetComponent<PanelReveal>();
        if (reveal != null)
        {
            if (open) reveal.Show();
            else reveal.Hide();
            return;
        }
        panel.SetActive(open);
    }

    // 낮 전환이 끝난 빌드 패널의 등장 연출을 시작한다.
    private void EnablePanel()
    {
        panelSlide.Open();
    }

    private void Awake()
    {
        heroPanel.SetActive(false);
        heroInventory.SetActive(false);
        classUpgradePanel.SetActive(false);

        // heroPanel/classUpgradePanel/heroInventory는 전용 스크립트가 없는 순수 GameObject라, OnEnable/OnDisable로
        // ExclusiveUiCoordinator에 알려줄 컴포넌트를 여기서 붙여준다(씬/프리팹을 직접 안 건드리기 위해).
        // 셋 다 등록해 둬야, 버튼 클릭이 Toggle()이든 OnHeroButton 등이든 어느 쪽을 거치든 상관없이
        // (OnEnable 기반이라 호출 경로를 안 타므로) 하나가 열리면 나머지가 항상 자동으로 닫힌다.
        if (heroPanel.GetComponent<ExclusivePanelPresence>() == null)
            heroPanel.AddComponent<ExclusivePanelPresence>();
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

    // ESC로 메뉴를 열지 말지 판단할 때 쓴다(UiManager) - 여기서 취소/닫을 게 있으면 ESC는
    // 메뉴를 여는 대신 그것부터 처리해야 하므로, Update()의 ESC 분기와 조건을 그대로 맞춘다.
    public bool HasEscapeCancelable =>
        view.HasArmedOrSelectedSkill
        || view.IsHolding
        || !view.IsOff
        || heroPanel.activeSelf
        || heroInventory.activeSelf
        || classUpgradePanel.activeSelf
        || (heroArchiveButton != null && heroArchiveButton.IsOpen)
        || (heroCreateAmountPanel != null && heroCreateAmountPanel.gameObject.activeInHierarchy);

    private void Update()
    {
        if (heroPanel.activeSelf && heroPanelCloser.ClickedOutside())
        {
            SetPanelOpen(heroPanel, false);
        }
        if (classUpgradePanel.activeSelf && classUpgradeCloser.ClickedOutside())
        {
            SetPanelOpen(classUpgradePanel, false);
        }
        if (heroInventory.activeSelf && view.IsOff && inventoryCloser.ClickedOutside())
        {
            heroInventory.SetActive(false);
        }

        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (selected != lastSelectedGameObject)
        {
            lastSelectedGameObject = selected;
            if (selected != null && (view.IsReplacing || view.IsRemoving))
            {
                Button clickedButton = selected.GetComponent<Button>();
                if (clickedButton != null
                    && clickedButton.GetComponent<ReplaceHeldLink>() == null
                    && clickedButton.GetComponent<RemoveHeldLink>() == null
                    && clickedButton.GetComponent<UIReplaceHeld>() == null
                    && clickedButton.GetComponent<UIRemoveHeld>() == null)
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

        if (keyboard[createHeroKey].wasPressedThisFrame && !TutorialInputGate.BlockHotkeys)
            OnHeroButton();

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
            || (heroArchiveButton != null && heroArchiveButton.IsOpen))
        {
            SetPanelOpen(heroPanel, false);
            heroInventory.SetActive(false);
            SetPanelOpen(classUpgradePanel, false);
            heroArchiveButton?.Close();
        }
    }

    // 재배치/제거 모드 중 다른 패널 버튼을 쓰면 그 모드를 끈다 - 클릭은 EventSystem의 선택 변경으로
    // Update()가 감지해 자동으로 꺼지지만, 단축키는 선택을 바꾸지 않아 그 감지를 타지 않는다.
    // 두 입력 경로의 결과가 갈리지 않도록 여기서 직접 꺼준다.
    private void ExitPlaceModeIfActive()
    {
        if (view.IsReplacing || view.IsRemoving) view.ClearMode();
    }

    public void OnHeroButton()
    {
        ExitPlaceModeIfActive();

        if (heroPanel.activeSelf)
        {
            SetPanelOpen(heroPanel, false);
        }
        else
        {
            SetPanelOpen(heroPanel, true);
            heroPanelCloser.MarkOpened();
            if (heroInventory.activeSelf) heroInventory.SetActive(false);
            if (classUpgradePanel.activeSelf)
            {
                SetPanelOpen(classUpgradePanel, false);
            }
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

    // 재배치/제거 모드로 들어가는 동안 인벤토리가 열려 있었다면 닫아둔다.
    private void CloseInventoryForMode()
    {
        if (!heroInventory.activeSelf) return;
        heroInventory.SetActive(false);
    }

    public void OnInventoryButton()
    {
        ExitPlaceModeIfActive();

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
        PanelPopIn.Play((RectTransform)heroInventory.transform);
        inventoryCloser.MarkOpened();
        if (classUpgradePanel.activeSelf)
        {
            SetPanelOpen(classUpgradePanel, false);
        }
        if (heroPanel.activeSelf) SetPanelOpen(heroPanel, false);
    }

    public void OnClassUpgradeButton()
    {
        if (TutorialInputGate.BlockHeroUpgradeOpen) return;

        ExitPlaceModeIfActive();

        if (classUpgradePanel.activeSelf)
            SetPanelOpen(classUpgradePanel, false);
        else
        {
            SetPanelOpen(classUpgradePanel, true);
            classUpgradeCloser.MarkOpened();
            if (heroInventory.activeSelf) heroInventory.SetActive(false);
            if (heroPanel.activeSelf) SetPanelOpen(heroPanel, false);
        }
    }

    public void OnOffButton()
    {
        view.ClearMode();
    }

    private static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
