using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

public class ResultState : IState
{
    private GameManager manager;
    private UiManager uiManager;

    public ResultState(GameManager manager, UiManager uiManager)
    {
        this.manager = manager;
        this.uiManager = uiManager;
    }

    public void Enter()
    {
        if(Time.timeScale > 0f)
            Time.timeScale = 0f;
        OpenPanel().Forget();

    }

    public void Exit()
    {
        if (manager.RequestSupport)
        {
            manager.ChangeRequest(false);
        }
        Time.timeScale = 1f;
    }

    public void Update() { }

    private async UniTaskVoid OpenPanel()
    {
        if (manager.RequestSupport)
        {
            await uiManager.OpenRequestSupportUi();
            MapRegistry.Instance?.UnlockNextModule();
            //manager.ExpandMapForce();
        }
        //if(카드 드래프트 플래그)
        //{
        //  //await 카드 드래프트 패널 오픈
        //}

        manager.OnDay();
    }
}
