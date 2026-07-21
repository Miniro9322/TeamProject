using System;
using UnityEngine;
using VContainer;

public class GameManager : MonoBehaviour
{
    private FSM fsm = new();

    private IState day;
    private IState night;
    private IState result;
    private IState gameover;

    private bool canBuild = false;
    private FacilityManager facilityManager;
    private UiManager uiManager;
    private WaveSpawner waveSpawner;
    private int dayCount = 0;
    [SerializeField] private int hp = 20;
    private bool requestSupport = false;
    public int DayCount => dayCount;
    public bool CanBuild => canBuild;
    public bool RequestSupport => requestSupport;
    private bool canSpawnEnemy = false;
    public bool CanSpawnEnemy => canSpawnEnemy;

    public event Action ChangeToDay;
    public event Action ChangeToNight;
    public event Action ExpandMap;

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
        gameover = new GameOverState(this);
        waveSpawner.EnemyAllClear += OnResult;
        fsm.ChangeState(day);
    }

    private void OnDestroy()
    {
        waveSpawner.EnemyAllClear -= OnResult;
    }

    public void OnNight()
    {
        fsm.ChangeState(night);
        ChangeToNight?.Invoke();
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

    public void ChangeRequest(bool value)
    {
        requestSupport = value;
    }

    public void ExpandMapForce()
    {
        ExpandMap?.Invoke();
    }

    public void ChangeCanSpawnEnemy(bool value)
    {
        canSpawnEnemy = value;
    }

    public void HpDamage()
    {
        hp--;
        Debug.Log($"현재 체력: {hp}");
        if(hp <= 0)
        {
            hp = 0;
            fsm.ChangeState(gameover);
        }
    }
}
