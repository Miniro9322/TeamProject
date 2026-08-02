using System.Collections.Generic;
using UnityEngine;


//타일은 고유 좌표와 인덱스를 가진다
//타일은 지형·점령·용도 3축으로 상태를 가진다
//타일이 알아야 하는 정보는 해당 타일 위 배치된 오브젝트와 저지되는 적이다.
//타일은 인덱스로 인해 인접한 타일로 접근할수있어야 한다.
[DisallowMultipleComponent]
public partial class Tile : MonoBehaviour
{
    [Tooltip("이 타일의 논리 상태(지형·점령·용도 3축). 인스펙터 또는 베이크(Tools/Map)로 저작.")]
    public TileState State = new();

    [Tooltip("적 스폰 지점 여부(경로 시작). MVP 임시 표식 — 이후 EnemyPath/MapData로 이관 예정.")]
    public bool isEnemySpawn;

    // ---- 런타임 캐시(직렬화하지 않음) ----
    private float _topY;

    /// <summary>이 타일이 속한 모듈 보드(아파트 문패의 동 번호). MapBoard.Build가 새긴다.
    /// 좌표는 모듈 로컬 0-base라 (Col,Row)만으로는 모듈을 특정할 수 없다 — 보드까지 있어야 완전한 주소.</summary>
    public MapBoard Board { get; private set; }

    //타일을 점유한 오브젝트
    public GameObject OccupantObject { get; private set; }
    public int BlockCapacity { get; private set; }

    // 내 위에 올라온 적
    private readonly List<GameObject> _enemies = new();

    //지금 이 타일 위에 있는 적들(읽기 전용). 없으면 빈 목록.
    public IReadOnlyList<GameObject> Enemies => _enemies;

    /// <summary>이 타일 위에 적이 하나라도 있는가.</summary>
    public bool HasEnemy => _enemies.Count > 0;
    public int EnemyCount => _enemies.Count;
    public int BlockedCount => Mathf.Min(_enemies.Count, BlockCapacity);

    public Vector2Int Coord => new(State.Col, State.Row);
    public TerrainType Terrain => State.Terrain;
    public bool IsGround => Terrain == TerrainType.Ground;
    public bool IsHigh => Terrain == TerrainType.High;
    public bool IsCore => Terrain == TerrainType.Core;
    public bool IsSpecial => Terrain == TerrainType.Special;
    public bool IsEnemyLane => State.EnemyLane;
    public bool IsEnemySpawn => isEnemySpawn;
    public bool HasUnit => OccupantObject != null;

 
    public GameObject UnitPrefab;
    public OccupantKind UnitKind = OccupantKind.MeleeHero;

    /// <summary>적 통행 가능 지형인가. 고지·빈 타일은 막힘, 지상·본진은 통행(설계: 고지=이동 차단).</summary>
    public bool Walkable => State.Terrain is TerrainType.Ground or TerrainType.Core;

    /// <summary>
    /// 타일 윗면 중앙의 월드 좌표(배치·경로 웨이포인트 기준).
    /// 높이는 MapBoard.Build가 SetTop으로 채워주는 캐시라 Build 이전에는 0이다 —
    /// 플레이를 거치지 않는 에디터 도구는 이 값을 읽지 말고 렌더러 바운즈에서 직접 구해야 한다.
    /// </summary>
    public Vector3 WorldTop => new(transform.position.x, _topY, transform.position.z);

    /// <summary>MapBoard가 스캔 시 윗면 높이를 캐시해 준다(WorldTop·배치·경로 기준).</summary>
    public void SetTop(float topY)
    {
        _topY = topY;
    }

    /// <summary>MapBoard.Build가 스캔 시 자기 자신을 새긴다(소유 보드 도장).</summary>
    public void SetBoard(MapBoard board)
    {
        Board = board;
    }

    //비어 있는 타일에 유닛을 배치(런타임 인스턴스를 기록). UnitPrefab은 인스펙터 저작값이라 건드리지 않는다.
    public void SetOccupant(GameObject go, OccupantKind kind)
    {
        OccupantObject = go;
        State.Occupant = kind;
        BlockCapacity = GetCapacity(go, kind);
        go.GetComponent<IPlaceAble>().OnBreak += SetBlockCapacity;
        go.GetComponent<IPlaceAble>().OnResur += SetBlockCapacity;
    }

    //배치되어 있는 유닛을 해제 후 반환.
    public GameObject ClearOccupant()
    {
        GameObject go = OccupantObject;

        go.GetComponent<IPlaceAble>().OnBreak -= SetBlockCapacity;
        go.GetComponent<IPlaceAble>().OnResur -= SetBlockCapacity;

        OccupantObject = null;
        State.Occupant = OccupantKind.None;
        BlockCapacity = 0; //언덕 타일은 점유 개념이 없으므로 근접만 우선 해당.

        return go;
    }

    public bool IsBlocked(GameObject enemy)
    {
        if (BlockCapacity <= 0 || enemy == null) return false;

        int index = _enemies.IndexOf(enemy);
        return index >= 0 && index < BlockCapacity;
    }

    //타일에 적 진입 등록
    public void AddEnemy(GameObject enemy)
    {
        if (!_enemies.Contains(enemy)) _enemies.Add(enemy);
    }

    //타일에 적 이탈 등록
    public void RemoveEnemy(GameObject enemy) => _enemies.Remove(enemy);
    

    private static int GetCapacity(GameObject go, OccupantKind kind)
    {
        if (kind != OccupantKind.MeleeHero)
        {
            return 0;
        }

        return go.GetComponent<Hero>().BlockCount;
    }

    private void SetBlockCapacity()
    {
        BlockCapacity = OccupantObject.GetComponent<Hero>().BlockCount;
    }
}
    
