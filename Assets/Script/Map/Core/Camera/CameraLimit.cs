using UnityEngine;

// 해금된 맵 모듈들을 모두 감싸는 월드 경계를 만든다. 카메라가 움직일 수 있는 범위의 원본이다.
public class CameraLimit
{
    public Bounds Area { get; private set; }

    // 경계를 만들었으면 true. 해금 모듈이 하나도 없으면 false(이때 Area는 의미 없음).
    public bool Build(MapRegistry registry)
    {
        bool hasArea = false;
        Bounds next = default;
        foreach (ModuleLogic module in registry.AllModules.Values)
        {
            // 해금된 모듈만 시야 경계에. 잠긴(미개방) 모듈은 제외한다.
            if (!module.IsUnlocked)
            {
                continue;
            }

            MapBoard board = module.GetComponent<MapBoard>();
            if (board.CellCount == 0)
            {
                board.Build();   // 타이밍상 아직 안 지어졌으면 여기서 지어 경계를 얻는다(FogController와 동일)
            }
            if (!hasArea)
            {
                next = board.WorldBounds;
                hasArea = true;
                continue;
            }
            next.Encapsulate(board.WorldBounds);
        }

        Area = next;
        return hasArea;
    }
}
