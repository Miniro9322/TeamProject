using UnityEngine;

public class DayState : IState
{
    private GameManager gameManager;
    private FacilityManager facilityManager;

    public DayState(GameManager manager, FacilityManager facilityManager)
    {
        gameManager = manager;
        this.facilityManager = facilityManager;
    }

    public void Enter()
    {
        gameManager.ChangeCanBuild(true);
        gameManager.IncreaseDayCount();
        Debug.Log($"{gameManager.DayCount}일차");
        facilityManager.SumProduct();
        if (gameManager.DayCount % 5 == 0)
            gameManager.ChangeRequest(true);
    }

    public void Exit()
    {
        gameManager.ChangeCanBuild(false);
    }

    public void Update() { }
}
