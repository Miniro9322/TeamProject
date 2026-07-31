using UnityEngine;

public class GameOverState : IState
{
    private GameManager gameManager;
    private UpgradeState upgradeState;

    public GameOverState(GameManager gameManager, UpgradeState upgradeState)
    {
        this.gameManager = gameManager;
        this.upgradeState = upgradeState;
    }

    public void Enter()
    {
        if(Time.timeScale > 0f)
            Time.timeScale = 0f;
        gameManager.isGameOver = true;

        int earnedPoints = Mathf.RoundToInt(gameManager.DayCount * 0.3f);
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
