using UnityEngine;

public class GameOverState : IState
{
    private GameManager gameManager;

    public GameOverState(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    public void Enter()
    {
        if(Time.timeScale > 0f)
            Time.timeScale = 0f;
        gameManager.isGameOver = true;
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
