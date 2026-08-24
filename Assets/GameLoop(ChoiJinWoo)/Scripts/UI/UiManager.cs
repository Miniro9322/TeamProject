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
    private GameSpeedUI gameSpeedUiComponent;
    private RectTransform gameSpeedUiRect;
    // 튜토리얼이 pauseTimeWhileActive 단계에서 Time.timeScale을 직접 안 건드리고 이 컴포넌트의
    // API(OnButtonClick)로 배속을 걸기 위해 참조한다 - gameSpeedUi는 UiManager 프리팹의 자식이라
    // 런타임에야 생성되므로 다른 컴포넌트가 인스펙터로 직접 드래그해 연결할 수 없다.
    public GameSpeedUI GameSpeedUi => gameSpeedUiComponent;
    // 같은 이유로 배속 패널의 RectTransform도 인스펙터 드래그가 불가능하다 - TutorialManager가
    // GameSpeedMention 스텝의 waypoint target을 런타임에 이 값으로 채워 넣는 데 쓴다.
    public RectTransform GameSpeedUiRect => gameSpeedUiRect;
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
        gameSpeedUiComponent = gameSpeedUi.GetComponent<GameSpeedUI>();
        gameSpeedUiRect = gameSpeedUi.GetComponent<RectTransform>();

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
