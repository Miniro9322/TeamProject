using System;
using System.Runtime.ConstrainedExecution;
using Cysharp.Threading.Tasks;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class UiManager : MonoBehaviour
{
    [SerializeField] private RequestSupportUi requestSupportUi;
    [SerializeField] private GameObject gamaOverUi;
    [SerializeField] private GameObject gameSpeedUi;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject guidePanel;
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI upgradeResourceText;
    [SerializeField] private Key MenuKey = Key.T;

    public byte UnlockedHero;
    public byte UnlockedEnemy;

    public event Action UnlockChanged;
    private Keyboard keyboard;

    private void Awake()
    {
        requestSupportUi.gameObject.SetActive(false);
        gamaOverUi.SetActive(false);
        gameSpeedUi.SetActive(false);
        menuPanel.SetActive(false);
        guidePanel.SetActive(false);
        requestSupportUi.OnUnlock += UpdateUnlock;
        keyboard = Keyboard.current;
    }

    private void Update()
    {
        if (keyboard == null) return;

        if (!TutorialInputGate.BlockEscapeClose && keyboard.escapeKey.wasPressedThisFrame)
        {

            if (menuPanel.activeSelf)
            {
                menuPanel.SetActive(false);
            }
        }

        if (keyboard[MenuKey].wasPressedThisFrame)
        {
            if(menuPanel.activeSelf)
                menuPanel.SetActive(false);
            else
                menuPanel.SetActive(true);
        }
    }

    public void ToggleGameSpeedUi(bool value)
    {
        gameSpeedUi.SetActive(value);
    }

    public async UniTask OpenRequestSupportUi()
    {
        requestSupportUi.gameObject.SetActive(true);
        await UniTask.WaitUntil(() => requestSupportUi.gameObject.activeSelf == false);
    }

    private void UpdateUnlock(byte unlock)
    {
        UnlockedHero |= unlock;
        UnlockChanged?.Invoke();
    }

    public void OpenGameOverUI(int daycount, int point)
    {
        dayText.text = $"Survive Day : {daycount}";
        upgradeResourceText.text = $"{point}";
        gamaOverUi.SetActive(true);
    }

    public void OnTitle()
    {
        SceneManager.LoadScene("TempTitle");
        Time.timeScale = 1f;
    }

    public void OnQuit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif

        Application.Quit();
    }

    public void OpenMenuPanel()
    {
        if(menuPanel.activeSelf == false)
            menuPanel.SetActive(true);
        else
            menuPanel.SetActive(false);
    }

    public void OpenGuide()
    {
        if (guidePanel.activeSelf == false)
            guidePanel.SetActive(true);
        else
            guidePanel.SetActive(false);
    }
}
