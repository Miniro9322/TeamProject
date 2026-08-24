using System;
using System.Collections.Generic;
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
    private UpgradeState upgradeState;
    [SerializeField] private int dayCount = 0;
    [SerializeField] private int hp = 20;
    [SerializeField] private List<BaseUpgradeData> hpUpgrades;
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
    private byte unlockedEnemy = 0b000111;
    public bool isGameOver = false;
    public event Action HpChanged;
    public int Hp => hp;
    public int todayHp;
    public bool perfactDefence = false;

    [Inject]
    private void Construct(UiManager uiManager, SpawnerManager waveSpawner, UpgradeState upgradeState)
    {
        this.uiManager = uiManager;
        this.waveSpawner = waveSpawner;
        this.upgradeState = upgradeState;
        unlockedHero = (byte)initialUnlockedHero;
        uiManager.UnlockedEnemy = unlockedEnemy;
        uiManager.UnlockedHero = unlockedHero;

        hp += (int)upgradeState.GetTotalEffect(hpUpgrades);

        day = new DayState(this);
        night = new NightState(this);
        result = new ResultState(this, uiManager);
        gameover = new GameOverState(this, upgradeState, UiManager);
        waveSpawner.AllRegionsClear += OnResult;
        fsm.ChangeState(day);
        uiManager.UnlockChanged += UpdateUnlock;
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
        HpChanged?.Invoke();
        if (hp <= 0)
        {
            hp = 0;
            fsm.ChangeState(gameover);
        }
    }

    public void UpdateUnlock()
    {
        unlockedHero = uiManager.UnlockedHero;
        unlockedEnemy = uiManager.UnlockedEnemy;
    }

    // 세이브 데이터로 진행 일차를 그대로 덮어쓴다 (로드 복원 전용)
    public void RestoreDayCount(int amount)
    {
        dayCount = amount;
    }

    // 세이브 데이터로 기지 체력을 그대로 덮어쓴다 (로드 복원 전용)
    public void RestoreHp(int amount)
    {
        hp = amount;
        HpChanged?.Invoke();
    }

    // 세이브 데이터로 오늘 아침 기준 체력을 그대로 덮어쓴다 (로드 복원 전용)
    public void RestoreTodayHp(int amount)
    {
        todayHp = amount;
    }

    // 세이브 데이터로 해금된 영웅 목록을 그대로 덮어쓴다 (로드 복원 전용)
    public void RestoreUnlockedHero(byte value)
    {
        unlockedHero = value;
        uiManager.UnlockedHero = value;
    }
}
