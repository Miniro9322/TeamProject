using System;
using System.Threading;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 모듈 개방 상태를 안개 셰이더 전역값(_FogAreas/_FogOpens/_FogCount)으로 밀어준다.
// 각 모듈의 월드 사각형은 그 모듈 보드의 WorldBounds(격자 기반)에서 얻는다.
public class FogController : MonoBehaviour
{
    [SerializeField] private MapRegistry registry;
    [SerializeField, Min(0.05f)] private float openDuration = 4f;   // 안개가 완전히 걷히는 시간(초)

    [Header("Debug/Test")]
    [Tooltip("끄면 안개 셰이더 효과를 완전히 없앤다(그리기 재료를 0개로 만듦). 테스트용 스위치.")]
    [SerializeField] private bool fogEnabled = true;

    public static event Action<int> RevealDone;

    [Header("Edge")]
    [Tooltip("잠긴 모듈 외곽 밖으로 안개를 더 밀어낼 칸 수. 경계 노이즈가 외곽 타일을 깎아먹는 걸 막는다.")]
    [SerializeField, Range(0f, 8f)] private float edgeCells = 1.5f;

    private const int MaxAreas = 8;
    private const float FullyOpenAmount = 1f;

    private static readonly int AreasId = Shader.PropertyToID("_FogAreas");
    private static readonly int OpensId = Shader.PropertyToID("_FogOpens");
    private static readonly int CountId = Shader.PropertyToID("_FogCount");
    private static readonly int MarginId = Shader.PropertyToID("_EdgeMargin");

    [Serializable]
    private struct StaticOpenArea
    {
        public Transform center;
        public Vector2 halfSize;
    }

    [Header("Static Open Areas")]
    [Tooltip("ModuleLogic이 아니지만 처음부터 항상 열려 있어야 하는 구역(예: 본진 마을). 수동 배선 전용.")]
    [SerializeField] private List<StaticOpenArea> staticOpenAreas = new();

    private readonly Vector4[] _areas = new Vector4[MaxAreas];
    private readonly float[] _opens = new float[MaxAreas];
    private readonly bool[] _revealed = new bool[MaxAreas];
    private readonly List<ModuleLogic> _modules = new();
    private int _count;
    private float _cellSize;

    private void Start()
    {
        BuildAreas();
        RegisterStaticAreas();
        ApplyEdgeMargin();
        ApplyEnabledState();
        BindModules();
        RevealUnlocked();
    }

    private void OnValidate()
    {
        ApplyEdgeMargin();
        ApplyEnabledState();
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

            _areas[_count] = ApplyModuleBackground(module, MeasureCellArea(board));

            _opens[_count] = 0f;
            _revealed[_count] = false;

            _modules.Add(module);
            _count++;
        }
    }

    // 이 모듈 자식 중 FogBackground가 붙은 장식 배경이 있으면 그 Renderer 범위까지 사각형을 넓혀서 합친다
    // (같은 open 값을 쓰게 되어 그 모듈이 해금될 때 배경도 같이 열린다). 수동 배선이 필요 없다.
    private static Vector4 ApplyModuleBackground(ModuleLogic module, Vector4 area)
    {
        foreach (FogBackground background in module.GetComponentsInChildren<FogBackground>())
        {
            Bounds bounds = background.GetComponent<Renderer>().bounds;
            area = UnionRect(area, new Vector4(bounds.center.x, bounds.center.z, bounds.extents.x, bounds.extents.z));
        }
        return area;
    }

    // 두 사각형(중심XZ, 반크기XZ)을 감싸는 가장 작은 사각형을 만든다.
    private static Vector4 UnionRect(Vector4 a, Vector4 b)
    {
        float minX = Mathf.Min(a.x - a.z, b.x - b.z);
        float maxX = Mathf.Max(a.x + a.z, b.x + b.z);
        float minZ = Mathf.Min(a.y - a.w, b.y - b.w);
        float maxZ = Mathf.Max(a.y + a.w, b.y + b.w);
        return new Vector4((minX + maxX) * 0.5f, (minZ + maxZ) * 0.5f, (maxX - minX) * 0.5f, (maxZ - minZ) * 0.5f);
    }

    // 인스펙터에 연결된 상시 오픈 구역을 안개 데이터에 등록한다.
    private void RegisterStaticAreas()
    {
        foreach (StaticOpenArea area in staticOpenAreas)
        {
            if (_count >= MaxAreas)
            {
                break;
            }
            if (area.center == null)
            {
                continue;
            }

            Vector3 position = area.center.position;
            _areas[_count] = new Vector4(position.x, position.z, area.halfSize.x, area.halfSize.y);
            _opens[_count] = FullyOpenAmount;
            _revealed[_count] = true;
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

    // 모듈이 이동된 뒤(해금 시 재배치 등) 캐시된 안개 영역을 그 모듈의 현재 위치로 다시 재고 반영한다.
    public void RefreshArea(ModuleLogic module)
    {
        int index = _modules.IndexOf(module);
        if (index < 0) return;

        MapBoard board = module.GetComponent<MapBoard>();
        _areas[index] = ApplyModuleBackground(module, MeasureCellArea(board));
        ApplyAreas();
    }

    private void RevealUnlocked()
    {
        for (int i = 0; i < _modules.Count; i++)
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
        for (int index = 0; index < _modules.Count; index++)
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

        NotifyDone(index);
    }

    private void NotifyDone(int index)
    {
        int moduleId = _modules[index].ModuleId; // 안개가 걷힌 지역 번호를 가져온다.
        RevealDone?.Invoke(moduleId); // 해당 지역의 안개 제거 완료를 알린다.
    }

    private void ApplyAreas()
    {
        Shader.SetGlobalVectorArray(AreasId, _areas);
        Shader.SetGlobalFloatArray(OpensId, _opens);
        Shader.SetGlobalInt(CountId, _count);
    }

    // 스위치가 꺼져 있으면 그릴 안개 재료를 0개로 만들어 효과를 완전히 없앤다.
    private void ApplyEnabledState()
    {
        if (fogEnabled)
        {
            ApplyAreas();
        }
        else
        {
            Shader.SetGlobalInt(CountId, 0);
        }
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
