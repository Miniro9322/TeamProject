using UnityEngine;

// 해금된 모듈들의 월드 경계(union)만 들고 있는다. 클램프 계산 자체는
// 카메라 기하(프러스텀)가 필요하므로 CameraRig가 Area/Ready를 읽어 수행한다.
public sealed class CameraLimit
{
    private Bounds _area;
    private bool _ready;

    public bool Ready => _ready;
    public Bounds Area => _area;

    public void Build(MapRegistry registry)
    {
        _ready = false;
        if (registry == null)
        {
            return;
        }

        bool hasArea = false;
        Bounds nextArea = default;
        foreach (ModuleLogic module in registry.AllModules.Values)
        {
            // 해금된 모듈만 시야 경계에. 잠긴(미개방) 모듈은 제외한다.
            if (module == null || !module.IsUnlocked)
            {
                continue;
            }

            MapBoard board = module.GetComponent<MapBoard>();
            if (board == null)
            {
                continue;
            }
            if (board.CellCount == 0)
            {
                board.Build(); // 타이밍상 아직 안 지어졌으면 여기서 지어 경계를 얻는다(FogController와 동일)
            }
            if (board.CellCount == 0)
            {
                continue;
            }

            Bounds boardArea = board.WorldBounds;
            if (!hasArea)
            {
                nextArea = boardArea;
                hasArea = true;
                continue;
            }

            nextArea.Encapsulate(boardArea);
        }

        if (!hasArea)
        {
            return;
        }

        _area = nextArea;
        _ready = true;
    }
}
