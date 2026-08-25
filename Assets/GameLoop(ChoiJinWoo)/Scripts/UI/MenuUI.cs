#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{
    [SerializeField] private SettingUI settingPanel;
    [SerializeField] private GameObject QuitAlert;
    [SerializeField] private Button openButton;

    private ClickOutsideCloser outsideCloser;

    private void Awake()
    {
        outsideCloser = new ClickOutsideCloser((RectTransform)transform, openButton != null ? openButton.transform : null);
    }

    private void OnEnable()
    {
        outsideCloser.MarkOpened();
        settingPanel.gameObject.SetActive(true);
        QuitAlert.SetActive(false);
    }

    private void Update()
    {
        if (outsideCloser.ShouldClose()) OnCloseButton();
    }

    public void OnCloseButton()
    {
        gameObject.SetActive(false);
    }

    public void OnQuitButton()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif

        Application.Quit();
    }

    public void OnQuitAlert()
    {
        QuitAlert.SetActive(true);
    }

    public void OnCancelQuit()
    {
        if (QuitAlert.activeSelf)
            QuitAlert.SetActive(false);
    }

    public void OnTitle()
    {
        SceneManager.LoadScene("TempTitle");
    }
}
