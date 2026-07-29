using UnityEditor;
using UnityEngine;

public class MenuUI : MonoBehaviour
{
    [SerializeField] private SettingUI settingPanel;
    [SerializeField] private GameObject QuitAlert;

    private void OnEnable()
    {
        settingPanel.gameObject.SetActive(true);
        QuitAlert.SetActive(false);
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
}
