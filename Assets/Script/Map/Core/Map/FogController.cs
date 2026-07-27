using System.Threading;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 모듈 개방 상태를 안개 셰이더 전역값(_FogAreas/_FogOpens/_FogCount)으로 밀어준다.
// 각 모듈의 월드 사각형은 그 모듈 보드의 WorldBounds(격자 기반)에서 얻는다.
public class FogController : MonoBehaviour
{
    [SerializeField] private MapRegistry registry;
    [SerializeField] private float openDuration = 4f;   // 안개가 완전히 걷히는 시간(초)

    [Header("Edge")]
    [Tooltip("잠긴 모듈 외곽 밖으로 안개를 더 밀어낼 칸 수. 경계 노이즈가 외곽 타일을 깎아먹는 걸 막는다.")]
    [SerializeField, Range(0f, 8f)] private float edgeCells = 1.5f;

    private const int MaxAreas = 8;

    private static readonly int AreasId = Shader.PropertyToID("_FogAreas");
    private static readonly int OpensId = Shader.PropertyToID("_FogOpens");
    private static readonly int CountId = Shader.PropertyToID("_FogCount");
    private static readonly int MarginId = Shader.PropertyToID("_EdgeMargin");

    private readonly Vector4[] _areas = new Vector4[MaxAreas];
    private readonly float[] _opens = new float[MaxAreas];
    private readonly bool[] _revealed = new bool[MaxAreas];
    private readonly List<ModuleLogic> _modules = new();
    private int _count;
    private float _cellSize;

    private void Start()
    {
        BuildAreas();
        ApplyEdgeMargin();
        ApplyAreas();
        BindModules();
        RevealUnlocked();
    }

    private void OnValidate()
    {
        ApplyEdgeMargin();
    }

    // 등록된 모듈마다 보드 경계를 사각형(xy=중심XZ, zw=반크기XZ)으로 담는다.
    private void BuildAreas()
    {
        foreach (ModuleLogic module in registry.AllModules.Values)
        {
            if (_count >= MaxAreas)
            {
                break;
            }

            MapBoard board = module.GetComponent<MapBoard>();
            if (board == null)
            {
                continue;
            }
            if (board.CellCount == 0)
            {
                board.Build();
            }
            if (_cellSize <= 0f)
            {
                _cellSize = board.CellSize;
            }

            _areas[_count] = MeasureCellArea(board);

            _opens[_count] = 0f;
            _revealed[_count] = false;

            _modules.Add(module);
            _count++;
        }
    }

    // 점유 셀 중심들의 격자 정렬 사각형(±반 칸). 렌더러 바운드가 아니라 칸에만 의존한다.
    private Vector4 MeasureCellArea(MapBoard board)
    {
        bool has = false;
        float minX = 0f, maxX = 0f, minZ = 0f, maxZ = 0f;
        foreach (Tile tile in board.Cells.Values)
        {
            Vector3 top = tile.WorldTop;
            if (!has)
            {
                minX = maxX = top.x;
                minZ = maxZ = top.z;
                has = true;
            }
            else
            {
                if (top.x < minX) { minX = top.x; }
                if (top.x > maxX) { maxX = top.x; }
                if (top.z < minZ) { minZ = top.z; }
                if (top.z > maxZ) { maxZ = top.z; }
            }
        }

        float half = board.CellSize * 0.5f;
        float centerX = (minX + maxX) * 0.5f;
        float centerZ = (minZ + maxZ) * 0.5f;
        float extentX = (maxX - minX) * 0.5f + half;
        float extentZ = (maxZ - minZ) * 0.5f + half;
        return new Vector4(centerX, centerZ, extentX, extentZ);
    }

    private void RevealUnlocked()
    {
        for (int i = 0; i < _count; i++)
        {
            if (_modules[i].IsUnlocked)
            {
                _revealed[i] = true;
                RevealArea(i).Forget();
            }
        }
    }

    private void BindModules()
    {
        foreach (ModuleLogic module in _modules)
        {
            module.OnStateChanged += ModuleChanged;
        }
    }

    // 개방된 모듈마다 한 번만 구멍을 연다. 이미 열린 뒤의 상태 변화(낮↔밤)는 무시.
    private void ModuleChanged(ModuleState state)
    {
        for (int index = 0; index < _count; index++)
        {
            if (_revealed[index])
            {
                continue;
            }
            if (!_modules[index].IsUnlocked)
            {
                continue;
            }

            _revealed[index] = true;
            RevealArea(index).Forget();
        }
    }

    private async UniTaskVoid RevealArea(int index)
    {
        CancellationToken token = this.GetCancellationTokenOnDestroy();
        float amount = _opens[index];
        while (amount < 1f)
        {
            amount += Time.deltaTime / openDuration;
            if (amount > 1f)
            {
                amount = 1f;
            }
            _opens[index] = amount;
            ApplyOpenState();
            await UniTask.Yield(token);
        }
    }

    private void ApplyAreas()
    {
        Shader.SetGlobalVectorArray(AreasId, _areas);
        Shader.SetGlobalFloatArray(OpensId, _opens);
        Shader.SetGlobalInt(CountId, _count);
    }

    private void ApplyEdgeMargin()
    {
        if (_cellSize <= 0f)
        {
            return;
        }
        Shader.SetGlobalFloat(MarginId, edgeCells * _cellSize);
    }

    private void ApplyOpenState()
    {
        Shader.SetGlobalFloatArray(OpensId, _opens);
    }

    private void OnDestroy()
    {
        foreach (ModuleLogic module in _modules)
        {
            if (module != null)
            {
                module.OnStateChanged -= ModuleChanged;
            }
        }
    }
}
