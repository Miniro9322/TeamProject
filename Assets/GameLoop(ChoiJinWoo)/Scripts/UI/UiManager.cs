using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using VContainer;

public class UiManager : MonoBehaviour
{
    [SerializeField] private BuildingPanel buildingUi;
    [SerializeField] private RequestSupportUi requestSupportUi;
    [SerializeField] private GameObject GamaOverUi;
    [SerializeField] private GameObject GameSpeedUi;
    public bool BuildingUiOpen => buildingUi.gameObject.activeSelf;
    public byte UnlockedHero;
    public byte UnlockedEnemy;

    public event Action UnlockChanged;

    private void Awake()
    {
        buildingUi.gameObject.SetActive(false);
        requestSupportUi.gameObject.SetActive(false);
        GamaOverUi.SetActive(false);
        GameSpeedUi.SetActive(false);
        requestSupportUi.OnUnlock += UpdateUnlock;
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (buildingUi.gameObject.activeSelf)
            {
                CloseBuildingUi();
            }
        }
    }

    public void ToggleGameSpeedUi(bool value)
    {
        GameSpeedUi.SetActive(value);
    }

    public void OpenBuildingUi(ProductionFacility facility)
    {
        buildingUi.gameObject.SetActive(true);
        buildingUi.InitFacilityInfo(facility);
    }

    public void CloseBuildingUi()
    {
        buildingUi.gameObject.SetActive(false);
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
