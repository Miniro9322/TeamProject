using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;

public class AddCitizen : MonoBehaviour
{
    private CitizenManager citizenManager;
    private ResourcesManager resourcesManager;
    private ResourceIconSet resourceIconSet;
    [SerializeField] private TMP_InputField amountInput;
    [SerializeField] private Image costIcon;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private int costAmount;
    [SerializeField] private RectTransform openButtonRect;
    [SerializeField] private RectTransform createButtonRect;

    public RectTransform CreateButtonRect => createButtonRect;
    private int amount = 0;
    private ClickOutsideCloser outsideCloser;
    private PanelReveal panelReveal;
    private InputAction rightClickAction;

    [Inject]
    private void Construct(CitizenManager citizenManager, ResourcesManager resourcesManager, ResourceIconSet resourceIconSet)
    {
        this.citizenManager = citizenManager;
        this.resourcesManager = resourcesManager;
        this.resourceIconSet = resourceIconSet;
    }

    private void Awake()
    {
        panelReveal = GetComponent<PanelReveal>();
        outsideCloser = new ClickOutsideCloser((RectTransform)transform, openButtonRect);

        if (panelReveal != null) transform.localScale = Vector3.zero;

        rightClickAction = new InputAction("AddCitizenRightClickClose", InputActionType.Button, "<Mouse>/rightButton");
        rightClickAction.performed += OnRightClickPerformed;
    }

    private void OnEnable()
    {
        GlobalUiInputSignals.ClickPerformed += HandleOutsideClick;
        GlobalUiInputSignals.EscapePerformed += HandleEscape;
        rightClickAction.Enable();
    }

    private void OnDisable()
    {
        GlobalUiInputSignals.ClickPerformed -= HandleOutsideClick;
        GlobalUiInputSignals.EscapePerformed -= HandleEscape;
        rightClickAction.Disable();
    }

    private void OnDestroy()
    {
        rightClickAction.performed -= OnRightClickPerformed;
        rightClickAction.Dispose();
    }

    public void OpenPanel()
    {
        if(gameObject.activeSelf)
            Close();
        else
        {
            if (panelReveal == null) panelReveal = GetComponent<PanelReveal>();
            if (panelReveal != null) panelReveal.Show();
            else gameObject.SetActive(true);
            outsideCloser.MarkOpened();
            amount = 0;
            UpdatePanel();
        }
    }

    public void Close()
    {
        if (panelReveal == null) panelReveal = GetComponent<PanelReveal>();
        if (panelReveal != null) panelReveal.Hide();
        else gameObject.SetActive(false);
    }

    private void OnRightClickPerformed(InputAction.CallbackContext context)
    {
        if (TutorialInputGate.BlockEscapeClose) return;
        Close();
    }

    private void HandleEscape()
    {
        if (TutorialInputGate.BlockEscapeClose) return;
        Close();
    }

    private void HandleOutsideClick()
    {
        if (outsideCloser.ClickedOutside())
        {
            Close();
        }
    }

    private (ProductionType Type, int Amount)[] GetCost()
    {
        return new (ProductionType Type, int Amount)[] { (ProductionType.Food, amount * -costAmount) };
    }

    private void UpdatePanel()
    {
        amountInput.text = $"{amount}";
        if (costIcon != null) costIcon.sprite = resourceIconSet.GetIcon(ProductionType.Food);
        costText.text = $"{amount * costAmount}";
        costText.color = resourcesManager.CheckResources(GetCost()) ? Color.white : Color.red;
    }

    public void ChangeAmount(string text)
    {
        int max = Mathf.Max(0, citizenManager.MaxCitizen - citizenManager.CurrentCitizen);

        if (string.IsNullOrEmpty(text))
        {
            amount = 0;
        }
        else if (long.TryParse(text, out long parsed))
        {
            amount = (int)Math.Clamp(parsed, 0, max);
        }
        else
        {
            amount = max;
        }

        UpdatePanel();
    }

    public void IncreaseAmount()
    {
        if(amount + citizenManager.CurrentCitizen < citizenManager.MaxCitizen)
            amount++;
        UpdatePanel();
        ClearButtonFocus();
    }

    public void DecreaseAmount()
    {
        if(amount > 0)
            amount--;
        UpdatePanel();
        ClearButtonFocus();
    }

    public void IncreaseTen()
    {
        if (amount + citizenManager.CurrentCitizen < citizenManager.MaxCitizen)
            amount = Mathf.Clamp(amount + 10, 0, citizenManager.MaxCitizen - citizenManager.CurrentCitizen);
        UpdatePanel();
        ClearButtonFocus();
    }

    public void DecreaseTen()
    {
        if (amount > 0)
            amount = Mathf.Clamp(amount - 10, 0, citizenManager.MaxCitizen - citizenManager.CurrentCitizen);
        UpdatePanel();
        ClearButtonFocus();
    }

    private void ClearButtonFocus()
    {
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void CreateCitizen()
    {
        var cost = GetCost();

        if (citizenManager.CheckCanIncreaseCitizen(amount) && resourcesManager.CheckResources(cost))
        {
            citizenManager.IncreaseCitizen(amount);
            resourcesManager.ProductChanged(cost);
        }

        Close();
    }
}
