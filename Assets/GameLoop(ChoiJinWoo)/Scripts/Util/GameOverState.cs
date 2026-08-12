using UnityEngine;

public class GameOverState : IState
{
    private GameManager gameManager;
    private UpgradeState upgradeState;
    private UiManager UiManager;

    public GameOverState(GameManager gameManager, UpgradeState upgradeState, UiManager uiManager)
    {
        this.gameManager = gameManager;
        this.upgradeState = upgradeState;
        UiManager = uiManager;
    }

    public void Enter()
    {
        if(Time.timeScale > 0f)
            Time.timeScale = 0f;
        gameManager.isGameOver = true;

        int earnedPoints = Mathf.RoundToInt(gameManager.DayCount * 0.3f);
        UiManager.OpenGameOverUI(gameManager.DayCount, earnedPoints);
        upgradeState.AddPoints(earnedPoints);
    }

    public void Exit()
    {
        Time.timeScale = 1f;
        gameManager.isGameOver = false;
    }

    public void Update()
    {

    }
}
