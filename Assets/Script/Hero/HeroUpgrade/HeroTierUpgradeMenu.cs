using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

public class HeroTierUpgradeMenu : MonoBehaviour, IClosablePanel
{
    [SerializeField] private HeroUpgradeMenuUI upgradeMenuUIPrefab;
    [SerializeField] private GameObject menuContent;
    [SerializeField] private Button openButton;
    private HeroUpgradeConfig config;
    private IObjectResolver resolver;
    private UiPanelStack panelStack;
    private ClickOutsideCloser outsideCloser;

    [Inject]
    private void Construct(HeroUpgradeConfig config, IObjectResolver resolver, UiPanelStack panelStack)
    {
        this.config = config;
        this.resolver = resolver;
        this.panelStack = panelStack;
    }

    private void Awake()
    {
        for (int i = 0; i < config.tierEntries.Count; i++)
        {
            HeroUpgradeMenuUI upgradeMenuUI = resolver.Instantiate(upgradeMenuUIPrefab, menuContent.transform);
            upgradeMenuUI.Set(i + 1);
        }

        if (openButton != null) openButton.onClick.AddListener(Toggle);
        outsideCloser = new ClickOutsideCloser((RectTransform)transform, openButton != null ? openButton.transform : null);
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        panelStack.Push(this);
        outsideCloser.MarkOpened();
    }

    private void OnDisable()
    {
        panelStack.Remove(this);
    }

    private void Update()
    {
        if (outsideCloser.ClickedOutside()) Close();
    }

    public void Toggle()
    {
        if (gameObject.activeSelf)
        {
            Close();
            return;
        }

        gameObject.SetActive(true);
        outsideCloser.MarkOpened();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}
