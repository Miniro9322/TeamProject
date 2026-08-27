using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

// 지역 안개가 걷힌 뒤 그 지역 기믹을 팝업과 외곽선으로 최초 1회만 안내한다.
public class GimmickRevealController : MonoBehaviour
{
    private static readonly List<Tile> EmptyTiles = new();

    [SerializeField] private MapRegistry registry;
    [SerializeField] private GimmickPopup gimmickPopup;
    [SerializeField] private Material edgeMaterial;
    [SerializeField] private float edgeWidth = 0.06f;
    [SerializeField] private float edgeLift = 0.025f;
    [SerializeField] private float highlightSeconds = 3f;

    private GimmickTileData tileData;
    private CampfireEdgeView edgeView;
    private int edgeVersion;

    // 기믹 기록장을 컨테이너에서 받아 둔다(씬 오브젝트가 아니라 컨테이너가 소유한다).
    [Inject]
    private void Construct(GimmickTileData tileData)
    {
        this.tileData = tileData;
    }

    // 외곽선 그리기 도구를 준비한다.
    private void Awake()
    {
        edgeView = new CampfireEdgeView(transform, edgeMaterial, edgeWidth, edgeLift);
    }

    private void OnEnable()
    {
        FogController.RevealDone += OnRegionRevealed;
    }

    private void OnDisable()
    {
        FogController.RevealDone -= OnRegionRevealed;
    }

    private void OnDestroy()
    {
        edgeView.Dispose();
    }

    // 안개가 걷힌 지역에 기믹 칸이 있으면 안내를 연출한다.
    private void OnRegionRevealed(int moduleId)
    {
        if (tileData.WasShown(moduleId))
        {
            return;
        }
        if (!registry.TryGetModuleLogic(moduleId, out ModuleLogic module))
        {
            return;
        }

        List<Tile> tiles = GimmickTileCalc.CollectTiles(module.GetComponent<MapBoard>());
        if (tiles.Count == 0)
        {
            return;
        }

        tileData.StoreTiles(moduleId, tiles);
        tileData.MarkShown(moduleId);
        PlayReveal(tiles);
    }

    // 팝업을 띄우고 대상 칸에 외곽선을 켠다.
    private void PlayReveal(List<Tile> tiles)
    {
        Tile sample = tiles[0];
        gimmickPopup.Show(GimmickKeyCalc.MakeNameKey(sample), GimmickKeyCalc.MakeDescKey(sample));
        ShowEdge(tiles);
        HideEdgeAsync().Forget();
    }

    // 넘겨받은 칸들로 외곽선을 새로 그린다.
    private void ShowEdge(List<Tile> tiles)
    {
        edgeVersion++;
        edgeView.Show(tiles, edgeVersion);
    }

    // 정해둔 시간이 지나면 외곽선을 지운다.
    private async UniTaskVoid HideEdgeAsync()
    {
        await UniTask.Delay(TimeSpan.FromSeconds(highlightSeconds),
            cancellationToken: this.GetCancellationTokenOnDestroy());
        ShowEdge(EmptyTiles);
    }
}
