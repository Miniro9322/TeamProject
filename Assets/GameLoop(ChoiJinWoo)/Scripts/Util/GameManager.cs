using System;
using UnityEngine;
using VContainer;

public class GameManager : MonoBehaviour
{
    FSM fsm = new();

    IState day;
    IState night;
    IState result;

    private bool canBuild = false;
    private FacilityManager facilityManager;
    private UiManager uiManager;
    private int dayCount = 0;
    public int DayCount => dayCount;
    public bool CanBuild => canBuild;

    public event Action<int> EnemySpawn;

    [Inject]
    private void Construct(FacilityManager facilityManager, UiManager uiManager)
    {
        this.facilityManager = facilityManager;
        this.uiManager = uiManager;
    }

    private void Start()
    {
        day = new DayState(this, facilityManager);
        night = new NightState(this);
        result = new ResultState(this, uiManager);
        fsm.ChangeState(day);
    }

    public void OnNight()
    {
        fsm.ChangeState(night);
    }

    public void OnDay()
    {
        fsm.ChangeState(day);
    }

    public void OnResult()
    {
        fsm.ChangeState(result);
    }

    public void ChangeCanBuild(bool value)
    {
        canBuild = value;
    }

    public void SpawnEnemy()
    {
        EnemySpawn?.Invoke(dayCount);
    }

    public void IncreaseDayCount()
    {
        dayCount++;
    }
}
