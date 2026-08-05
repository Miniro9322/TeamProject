using System;
using Cysharp.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class UiManager : MonoBehaviour
{
    [SerializeField] private RequestSupportUi requestSupportUi;
    [SerializeField] private GameObject GamaOverUi;
    [SerializeField] private GameObject GameSpeedUi;
    [SerializeField] private GameObject MenuPanel;
    [SerializeField] private Key MenuKey = Key.T;

    public byte UnlockedHero;
    public byte UnlockedEnemy;

    public event Action UnlockChanged;
    private Keyboard keyboard;

    private void Awake()
    {
        requestSupportUi.gameObject.SetActive(false);
        GamaOverUi.SetActive(false);
        GameSpeedUi.SetActive(false);
        MenuPanel.SetActive(false);
        requestSupportUi.OnUnlock += UpdateUnlock;
        keyboard = Keyboard.current;
    }

    private void Update()
    {
        if (keyboard == null) return;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {

            if (MenuPanel.activeSelf)
            {
                MenuPanel.SetActive(false);
            }
        }

        if (keyboard[MenuKey].wasPressedThisFrame)
        {
            if(MenuPanel.activeSelf)
                MenuPanel.SetActive(false);
            else
                MenuPanel.SetActive(true);
        }
    }

    public void ToggleGameSpeedUi(bool value)
    {
        GameSpeedUi.SetActive(value);
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

    public void OpenGameOverUI()
    {
        GamaOverUi.SetActive(true);
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
}
