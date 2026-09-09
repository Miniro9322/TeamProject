using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

/// <summary>
/// 튜토리얼이 끝난 뒤 플레이어가 튜토리얼 중에 만든 것(영웅/건물/자원/시민)을
/// 되돌리는 "0일차 리셋"만 담당한다. 재시작(DebugRestart)용 상태 초기화도 여기 둔다.
/// </summary>
public class TutorialRollback
{
    private readonly HeroRoster heroRoster;
    private readonly BaseConstructor baseConstructor;
    private readonly ResourcesManager resourcesManager;
    private readonly CitizenManager citizenManager;
    private readonly GameManager gameManager;
    private readonly RegionOverviewPanel regionOverviewPanel;
    private readonly MapGame mapGame;
    private readonly SaveManager saveManager;
    private readonly TutorialState state;

    [Inject]
    public TutorialRollback(HeroRoster heroRoster, BaseConstructor baseConstructor,
        ResourcesManager resourcesManager, CitizenManager citizenManager, GameManager gameManager,
        RegionOverviewPanel regionOverviewPanel, MapGame mapGame, SaveManager saveManager,
        TutorialState state)
    {
        this.heroRoster = heroRoster;
        this.baseConstructor = baseConstructor;
        this.resourcesManager = resourcesManager;
        this.citizenManager = citizenManager;
        this.gameManager = gameManager;
        this.regionOverviewPanel = regionOverviewPanel;
        this.mapGame = mapGame;
        this.saveManager = saveManager;
        this.state = state;
    }

    /// <summary>0일차 → 진짜 1일차 전환 시 한 번 실행. 완료 메시지 표시는 호출측(TutorialManager)이 이어서 한다.</summary>
    public async UniTask RunAsync()
    {
        await UniTask.Yield();

        ResetHeroes();
        ResetBuildings();
        resourcesManager.Reset();
        citizenManager.Reset();
        gameManager.ResetHpToFull();

        gameManager.perfactDefence = false;

        state.MarkSeen();

        TutorialInputGate.BlockSave = false;
        saveManager.SaveNow();
    }

    /// <summary>DebugRestart 전용: 튜토리얼을 처음부터 다시 돌리기 위한 상태 초기화.</summary>
    public void PrepareReplay()
    {
        state.Reset();
        gameManager.ResetDayCountForTutorialReplay();
    }

    private void ResetHeroes()
    {
        foreach (var entry in new List<HeroRosterEntry>(heroRoster.Entries))
        {
            GameObject unit = entry.PlacedUnit;
            if (unit != null)
            {
                if (mapGame.Units.TryGetArea(unit, out PlacementArea area))
                {
                    AreaPlace.Remove(area);
                    mapGame.Units.Remove(unit);
                }

                if (unit.TryGetComponent(out Hero hero))
                {
                    hero.PrepareForDespawn();
                    PoolManager.Instance.Despawn(unit);
                }
                else
                {
                    Object.Destroy(unit);
                }
            }

            citizenManager.FreeCitizenForHero(entry.CitizenCost);
            heroRoster.Remove(entry);
        }
    }

    private void ResetBuildings()
    {
        foreach (var region in regionOverviewPanel.Regions)
        {
            for (int i = 0; i < region.Slots.Count; i++)
            {
                if (!region.Slots[i].IsEmpty) baseConstructor.Demolish(region, i);
            }
        }
    }
}
