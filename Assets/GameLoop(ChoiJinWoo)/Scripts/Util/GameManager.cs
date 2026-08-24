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
    [SerializeField] private int dayCount; // Construct()에서 튜토리얼 진행 여부에 따라 -1 또는 0으로 초기화
    [SerializeField] private int hp = 20;
    private int initialHp; // 0일차 튜토리얼 리셋용 스냅샷
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
    private void Construct(UiManager uiManager, SpawnerManager waveSpawner, UpgradeState upgradeState, TutorialState tutorialState)
    {
        this.uiManager = uiManager;
        this.waveSpawner = waveSpawner;
        this.upgradeState = upgradeState;
        unlockedHero = (byte)initialUnlockedHero;
        uiManager.UnlockedEnemy = unlockedEnemy;
        uiManager.UnlockedHero = unlockedHero;

        hp += (int)upgradeState.GetTotalEffect(hpUpgrades);
        initialHp = hp;

        // 튜토리얼을 이번 세션에서 처음 보는 거면 0일차(연습)부터, 이미 본 적 있으면 0일차 없이 곧장 1일차부터.
        dayCount = tutorialState.Seen ? 0 : -1;

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

    // 테스트용 - TutorialManager.DebugRestart()가 0일차 흐름을 다시 재현할 때 쓴다. dayCount를
    // 안 맞춰주면 이미 진행된 실제 날짜의 웨이브가 나가고, 밤이 끝난 뒤 0일차 리셋 타이밍도
    // 어긋난다 - 지금 낮을 "0일차"로 다시 취급하도록 되돌린다.
    public void ResetDayCountForTutorialReplay()
    {
        dayCount = 0;
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

    // 0일차 튜토리얼 밤 전투에서 입은 데미지를 되돌린다.
    public void ResetHpToFull()
    {
        hp = initialHp;
        HpChanged?.Invoke();
    }
}
