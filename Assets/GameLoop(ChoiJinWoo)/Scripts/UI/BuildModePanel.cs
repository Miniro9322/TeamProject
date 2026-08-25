using UnityEngine;
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
    [SerializeField] private Key closeKey = Key.Escape;
    [SerializeField] private Key upgradeKey = Key.U;
    [SerializeField] private Key replaceKey = Key.R;
    [SerializeField] private Key removeKey = Key.E;
    [SerializeField] private Key inventoryKey = Key.I;
    private Keyboard keyboard;
    private ClickOutsideCloser heroPanelCloser;
    private ClickOutsideCloser inventoryCloser;
    private ClickOutsideCloser classUpgradeCloser;

    private void Awake()
    {
        heroPanel.SetActive(false);
        heroInventory.SetActive(false);
        classUpgradePanel.SetActive(false);

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

        if (keyboard == null) return;

        if (keyboard[upgradeKey].wasPressedThisFrame)
            OnClassUpgradeButton();

        if (keyboard[replaceKey].wasPressedThisFrame)
            OnReplaceButton();

        if (keyboard[removeKey].wasPressedThisFrame)
            OnRemoveButton();

        if (keyboard[inventoryKey].wasPressedThisFrame)
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
        gameObject.SetActive(false);
    }

    private void EnablePanel()
    {
        gameObject.SetActive(true);
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
