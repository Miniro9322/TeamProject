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
    private WaveSpawner waveSpawner;
    private int dayCount = 0;
    public int DayCount => dayCount;
    public bool CanBuild => canBuild;

    public event Action ChangeToDay;

    [Inject]
    private void Construct(FacilityManager facilityManager, UiManager uiManager, WaveSpawner waveSpawner)
    {
        this.facilityManager = facilityManager;
        this.uiManager = uiManager;
        this.waveSpawner = waveSpawner;
    }

    private void Start()
    {
        day = new DayState(this, facilityManager);
        night = new NightState(this);
        result = new ResultState(this, uiManager);
        //waveSpawner.EnemyAllClear += OnResult;
        fsm.ChangeState(day);
    }

    private void OnDestroy()
    {
        //waveSpawner.EnemyAllClear -= OnResult;
    }

    public void OnNight()
    {
        fsm.ChangeState(night);
    }

    public void OnDay()
    {
        fsm.ChangeState(day);
        ChangeToDay?.Invoke();
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
        waveSpawner.SpawnWave(DayCount);
    }

    public void IncreaseDayCount()
    {
        dayCount++;
    }
}
