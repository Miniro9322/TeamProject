using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class NightState : IState
{
    private GameManager gameManager;
    public NightState(GameManager manager)
    {
        this.gameManager = manager;
    }

    public void Enter()
    {
        TestCode().Forget();
    }

    public void Exit()
    {
        gameManager.ChangeCanSpawnEnemy(false);
    }

    public void Update()
    {
        
    }

    private async UniTaskVoid TestCode()
    {
        await UniTask.WaitUntil(() => gameManager.CanSpawnEnemy);
        gameManager.SpawnEnemy();
        await UniTask.WaitForSeconds(3);
        gameManager.OnResult();
    }
}
