using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

public class UiManager : MonoBehaviour
{
    [SerializeField] private BuildingPanel buildingUi;
    [SerializeField] private RequestSupportUi requestSupportUi;
    public bool BuildingUiOpen => buildingUi.gameObject.activeSelf;

    private void Awake()
    {
        buildingUi.gameObject.SetActive(false);
        requestSupportUi.gameObject.SetActive(false);
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
        Debug.Log(requestSupportUi.gameObject.activeSelf);
        await UniTask.WaitUntil(() => requestSupportUi.gameObject.activeSelf == false);
    }
}
