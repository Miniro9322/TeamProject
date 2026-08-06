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
            // 해금 유닛 선택 창은 더 이상 쓰지 않음 — 지역은 정해진 순서대로 자동 해금되고,
            // HeroCreateManager의 확률 테이블은 지역 해금 이벤트에 그대로 반응해 올라간다.
            // await uiManager.OpenRequestSupportUi();
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
