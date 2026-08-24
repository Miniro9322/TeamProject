using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

public class HeroClassUpgradeMenu : MonoBehaviour
{
    [SerializeField] private HeroClassUpgradeMenuUI upgradeMenuUIPrefab;
    [SerializeField] private GameObject menuContent;
    //[SerializeField] private Button openButton;
    private HeroClassUpgradeConfig config;
    private IObjectResolver resolver;
    private UiPanelStack panelStack;

    [Inject]
    private void Construct(HeroClassUpgradeConfig config, IObjectResolver resolver, UiPanelStack panelStack)
    {
        this.config = config;
        this.resolver = resolver;
        this.panelStack = panelStack;
    }

    private void Awake()
    {
        for (int i = 0; i < config.classEntries.Count; i++)
        {
            HeroClassUpgradeMenuUI upgradeMenuUI = resolver.Instantiate(upgradeMenuUIPrefab, menuContent.transform);
            upgradeMenuUI.Set(config.classEntries[i].heroType);
        }

        //if (openButton != null) openButton.onClick.AddListener(Toggle);
        //outsideCloser = new ClickOutsideCloser((RectTransform)transform, openButton != null ? openButton.transform : null);
        //gameObject.SetActive(false);
    }

    //private void OnEnable()
    //{
    //    panelStack.Push(this);
    //    //outsideCloser.MarkOpened();
    //}

    //private void OnDisable()
    //{
    //    panelStack.Remove(this);
    //}

    //private void Update()
    //{
    //    //if (outsideCloser.ClickedOutside()) Close();
    //}

    //public void Toggle()
    //{
    //    if (gameObject.activeSelf)
    //    {
    //        Close();
    //        return;
    //    }

    //    gameObject.SetActive(true);
    //    //outsideCloser.MarkOpened();
    //}

    //public void Close()
    //{
    //    gameObject.SetActive(false);
    //}
}
