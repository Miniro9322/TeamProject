using UnityEngine;

public class DayState : IState
{
    private GameManager gameManager;

    public DayState(GameManager manager)
    {
        gameManager = manager;
    }

    public void Enter()
    {
        gameManager.UiManager.ToggleGameSpeedUi(false);
        gameManager.ChangeCanBuild(true);
        gameManager.IncreaseDayCount();
        Debug.Log($"{gameManager.DayCount}일차");
        if (gameManager.DayCount % 5 == 0)
        {
            gameManager.ChangeRequest(true);
        }
    }

    public void Exit()
    {
        gameManager.ChangeCanBuild(false);
    }

    public void Update() { }
}
