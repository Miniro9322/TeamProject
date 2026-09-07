using System;
using System.Collections.Generic;
using UnityEngine;
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
    [Tooltip("에디터에서 타이틀을 거치지 않고 MainScene을 바로 실행해 디버깅할 때 체크 - " +
        "Construct()가 dayCount를 튜토리얼 진행 여부로 덮어쓰지 않고 위 인스펙터 값을 그대로 쓴다. " +
        "DayState.Enter()가 진입하며 1 증가시키니, 원하는 날짜보다 1 작게 넣어야 한다.")]
    [SerializeField] private bool debugKeepInspectorDayCount;
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

    private string gameSeed = Guid.NewGuid().ToString("N");
    private int heroDrawMeleeCount;
    private int heroDrawRangedCount;
    private int[] heroCombineMeleeCounts = new int[3];
    private int[] heroCombineRangedCounts = new int[3];
    private int heroesCreatedToday;

    public string GameSeed => gameSeed;
    public int HeroDrawMeleeCount => heroDrawMeleeCount;
    public int HeroDrawRangedCount => heroDrawRangedCount;
    public int[] HeroCombineMeleeCounts => heroCombineMeleeCounts;
    public int[] HeroCombineRangedCounts => heroCombineRangedCounts;
    public int HeroesCreatedToday => heroesCreatedToday;

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

#if UNITY_EDITOR
        if (!debugKeepInspectorDayCount)
        {
            dayCount = tutorialState.Seen ? 0 : -1;
        }
#else
        dayCount = tutorialState.Seen ? 0 : -1;
#endif

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
        heroesCreatedToday = 0;
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
    public void RestoreDayCount(int amount)
    {
#if UNITY_EDITOR
        if (debugKeepInspectorDayCount) return;
#endif
        dayCount = amount;
    }

    public void RestoreHp(int amount)
    {
        hp = amount;
        HpChanged?.Invoke();
    }

    public void RestoreTodayHp(int amount)
    {
        todayHp = amount;
    }

    public void RestoreUnlockedHero(byte value)
    {
        unlockedHero = value;
        uiManager.UnlockedHero = value;
    }

    public void RestoreGameSeed(string seed)
    {
        gameSeed = string.IsNullOrEmpty(seed) ? Guid.NewGuid().ToString("N") : seed;
    }

    public void RestoreHeroDrawCounts(int meleeCount, int rangedCount)
    {
        heroDrawMeleeCount = meleeCount;
        heroDrawRangedCount = rangedCount;
    }

    public void AddHeroesCreatedToday(int count)
    {
        heroesCreatedToday += count;
    }

    public void RestoreHeroesCreatedToday(int count)
    {
        heroesCreatedToday = count;
    }

    public (string seed, int count) ConsumeHeroDraw(OccupantKind kind)
    {
        if (kind == OccupantKind.RangedHero)
        {
            heroDrawRangedCount++;
            return (gameSeed, heroDrawRangedCount);
        }

        heroDrawMeleeCount++;
        return (gameSeed, heroDrawMeleeCount);
    }

    public void RestoreHeroCombineCounts(int[] meleeCounts, int[] rangedCounts)
    {
        heroCombineMeleeCounts = meleeCounts ?? new int[3];
        heroCombineRangedCounts = rangedCounts ?? new int[3];
    }

    public (string seed, int count) ConsumeHeroCombine(OccupantKind kind, int tier)
    {
        int[] counts = kind == OccupantKind.RangedHero ? heroCombineRangedCounts : heroCombineMeleeCounts;
        int index = Mathf.Clamp(tier - 1, 0, counts.Length - 1);
        counts[index]++;
        return (gameSeed, counts[index]);
    }

    public void ResetHpToFull()
    {
        hp = initialHp;
        HpChanged?.Invoke();
    }
}
