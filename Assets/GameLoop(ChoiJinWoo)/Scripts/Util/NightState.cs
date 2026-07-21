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
        Night().Forget();
    }

    public void Exit()
    {
        gameManager.ChangeCanSpawnEnemy(false);
    }

    public void Update()
    {
        
    }

    private async UniTaskVoid Night()
    {
        await UniTask.WaitUntil(() => gameManager.CanSpawnEnemy);
        gameManager.SpawnEnemy();
    }
}
