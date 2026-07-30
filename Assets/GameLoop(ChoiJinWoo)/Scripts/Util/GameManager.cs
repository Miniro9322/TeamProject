using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

public class GameManager : MonoBehaviour
{
    private FSM fsm = new();

    private IState day;
    private IState night;
    private IState result;
    private IState gameover;

    private bool canBuild = true;
    private UiManager uiManager;
    public UiManager UiManager => uiManager;
    private SpawnerManager waveSpawner;
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

    [SerializeField] private HeroType initialUnlockedHero = HeroType.SwordMan | HeroType.Archer;
    private byte unlockedHero;
    public byte UnlockHero => unlockedHero;
    private byte UnlockedEnemy = 0b000111;
    public bool isGameOver = false;

    [Inject]
    private void Construct(UiManager uiManager, SpawnerManager waveSpawner)
    {
        this.uiManager = uiManager;
        this.waveSpawner = waveSpawner;
        unlockedHero = (byte)initialUnlockedHero;
        uiManager.UnlockedEnemy = UnlockedEnemy;
        uiManager.UnlockedHero = unlockedHero;
    }

    private void Start()
    {
        day = new DayState(this);
        night = new NightState(this);
        result = new ResultState(this, uiManager);
        gameover = new GameOverState(this);
        waveSpawner.AllRegionsClear += OnResult;
        fsm.ChangeState(day);
        uiManager.UnlockChanged += UpdateUnlock;
    }

    private void Update()
    {
        if (Keyboard.current.cKey.wasPressedThisFrame)
            requestSupport = true;
    }

    private void OnDestroy()
    {
        waveSpawner.AllRegionsClear -= OnResult;
        uiManager.UnlockChanged -= UpdateUnlock;
    }

    public void OnNight()
    {
        if (isGameOver)
            return;

        fsm.ChangeState(night);
        ChangeToNight?.Invoke();
    }

    public void OnDay()
    {
        if (isGameOver)
            return;

        fsm.ChangeState(day);
        ChangeToDay?.Invoke();
    }

    public void OnResult()
    {
        if (isGameOver)
            return;

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

    public void HpDamage(EnemyClass enemyclass)
    {
        switch (enemyclass)
        {
            case EnemyClass.Normal:
                hp--;
                break;
            case EnemyClass.Elite:
                hp -= 2;
                break;
            case EnemyClass.Boss:
                hp = 0;
                break;
        }
        Debug.Log($"현재 체력: {hp}");
        if(hp <= 0)
        {
            hp = 0;
            fsm.ChangeState(gameover);
            uiManager.OpenGameOverUI();
        }
    }

    public void UpdateUnlock()
    {
        unlockedHero = uiManager.UnlockedHero;
        UnlockedEnemy = uiManager.UnlockedEnemy;
    }
}
