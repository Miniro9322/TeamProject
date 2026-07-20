using System;
using System.Collections.Generic;
using CsvHelper;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

public class MapGame : MonoBehaviour
{
    [Serializable]
    public class Placeable
    {
        public string label = "유닛";
        public GameObject prefab;
        public OccupantKind kind = OccupantKind.MeleeHero; 
        [Min(0)] public int attackRange;
    }

    private enum PlaceMode { Off, Place, Remove }

    [Header("References")]
    public MapBoard board;

    [Header("Placement")]
    [Tooltip("배치할 프리팹 목록.")]
    public List<Placeable> palette = new();
    public float placeYOffset = 0f;

    /// <summary>배치 직후 호출(종류, 생성된 오브젝트, 타일). 팀원 로직 연결용 훅.</summary>
    public event Action<OccupantKind, GameObject, Tile> Placed;
    public event Action OnPlaced;

    private Camera _cam;
    private PlaceMode _mode = PlaceMode.Off;
    private int _paletteIndex;

    private readonly List<GameObject> _placed = new();
    private readonly Dictionary<GameObject, int> _ranges = new();

    private Tile _selectedTile;
    private bool _inputBlocked;
    private string _status = "";

    // ---- 재배치(이동) 상태 ----
    private GameObject _held;      // 집어든 유닛(이동 프리뷰). 보드에서 뗀 채 포인터를 따라간다.
    private Tile _heldFrom;        // 집은 출발 타일(제자리 클릭 판별·취소용).
    private OccupantKind _heldKind;
    private int _heldRange;
    private Vector2 _pressPos;     // 눌린 화면 좌표(클릭 vs 드래그 판별용).
    [SerializeField] private float _dragPixels = 8f; // 이 픽셀 이상 움직이면 드래그로 간주.

    public bool IsHolding => _held != null;
    public string HeldInfo => _held == null ? "" : $"{_heldKind} 이동 중 (출발 {_heldFrom.Coord}, 사거리 {_heldRange})";

    public bool InputBlocked => _inputBlocked;
    public Tile HoverTile => PickCellUnderPointer();
    public bool IsPlacing => _mode == PlaceMode.Place;
    public OccupantKind PlacingKind => CurrentKind();
    public int PlacingRange => PreviewRange(CurrentKind());

    public string Status => _status;
    public Tile Selected => _selectedTile;
    public string Mode => _mode.ToString();
    public int UnitIndex => _mode == PlaceMode.Place ? _paletteIndex : -1;
    public IReadOnlyList<Placeable> Items => Palette();

    //테스트용 코드
    private VContainer.IObjectResolver resolver;
    private ResourcesManager resourcesManager;
    private BuildingPool pool;
    private UiManager uiManager;
    private GameManager gameManager;
    private CitizenManager citizenManager;

    [Inject]
    private void Construct(VContainer.IObjectResolver resolver, ResourcesManager resourcesManager, BuildingPool buildingPool, UiManager uiManager, GameManager gameManager, CitizenManager citizenManager)
    {
        this.resolver = resolver;
        this.resourcesManager = resourcesManager;
        this.pool = buildingPool;
        this.uiManager = uiManager;
        this.gameManager = gameManager;
        this.citizenManager = citizenManager;
    }

    //끝

    private void Awake()
    {
        _cam = Camera.main;
    }

    private void Start()
    {
        if (board == null)
        {
            _status = "MapBoard 없음";
            Debug.LogError("[MapGame] 씬에 MapBoard가 없습니다. 빈 오브젝트에 MapBoard 컴포넌트를 추가하세요.", this);
            return;
        }
    }

    private void Update()
    {
        if (_cam == null) _cam = Camera.main;
        HandleInput();
        FollowHeld();
    }

    private List<Placeable> Palette()
    {
        return palette;
    }

    private OccupantKind CurrentKind()
    {
        List<Placeable> pal = Palette();
        return (_paletteIndex >= 0 && _paletteIndex < pal.Count) ? pal[_paletteIndex].kind : OccupantKind.MeleeHero;
    }

    private void HandleInput()
    {
        if (_inputBlocked || Mouse.current == null)
        {
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame) OnPress();
        if (Mouse.current.leftButton.wasReleasedThisFrame) OnRelease();
    }

    // 누르는 순간: 집은 상태면 내려놓기, 배치된 유닛을 누르면 집기(제거 모드 제외), 그 외 모드별 처리.
    private void OnPress()
    {
        Tile tile = PickCellUnderPointer();
        if (_held != null)
        {
            Drop(HeldTarget()); // 그리드 밖이어도 프리뷰가 놓인 가장자리 타일로 내려놓기.
            return;
        }
        if (tile == null)
        {
            //테스트용 코드
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (uiManager.BuildingUiOpen)
            {
                uiManager.CloseBuildingUi();
            }
            //끝
            return;
        }
        //테스트용 코드
        if (tile.State.Occupant == OccupantKind.Resource && _mode != PlaceMode.Remove && gameManager.CanBuild)
        {
            uiManager.OpenBuildingUi(tile.OccupantObject.GetComponent<ProductionFacility>());
        }
        else
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (uiManager.BuildingUiOpen)
            {
                uiManager.CloseBuildingUi();
            }
        }
        //끝

        // 어느 모드든 배치된 유닛을 누르면 집어서 재배치한다(명시적 제거 모드만 예외).
        if (tile.HasUnit && _mode != PlaceMode.Remove)
        {
            PickUpUnit(tile);
            return;
        }

        switch (_mode)
        {
            case PlaceMode.Remove: RemoveAt(tile); break;
            case PlaceMode.Place: PlaceUnit(tile); break;
            default: SelectTile(tile); break;
        }
    }

    // 떼는 순간: 집은 채 드래그였다면 그 타일에 내려놓는다.
    // 제자리 클릭이면 집은 채 유지 → 다음 클릭으로 내려놓기(클릭-클릭 방식 지원).
    private void OnRelease()
    {
        if (_held == null) return;

        Tile tile = PickCellUnderPointer();   // 드래그 판정은 실제 커서 타일 기준
        if (IsDrag(tile)) Drop(HeldTarget()); // 내려놓기는 클램프된 목표 타일로
    }

    // 다른 타일 위에서 뗐거나 화면상 충분히 움직였으면 드래그로 본다(제자리 클릭과 구분).
    private bool IsDrag(Tile releaseTile)
    {
        if (releaseTile != null && releaseTile != _heldFrom) return true;
        Vector2 moved = Mouse.current.position.ReadValue() - _pressPos;
        return moved.magnitude > _dragPixels;
    }

    private void SelectTile(Tile tile)
    {
        _selectedTile = tile;
        _status = $"{tile.Coord} 선택";
    }

    private void PlaceUnit(Tile tile)//배치 모드에서 마우스 클릭 시 호출. 배치 조건 확인 후 배치 진행.
    {
        if(!TryGetSlot(out Placeable slot)) return; // 배치할 프리팹 정보
        
        if(!CanPlaceUnit(tile, slot)) return;       // 배치 가능 여부 확인
        
        //테스트용 코드
        if (!gameManager.CanBuild)
        {
            Debug.Log("밤에는 배치할 수 없습니다.");
            return;
        }

        if (!CheckCanBuild(slot.label))
        {
            ClearMode(); // 지금 실제로 지으려는 대상이 부족할 때만 모드 종료
            _status = $"{slot.label} 자원 부족";
            return;
        }
        //끝

        GameObject UnitObject = CreateUnit(slot);   // 배치할 오브젝트 생성

        BindBoard(UnitObject); // 생성한 오브젝트에 MapBoard 참조 전달
        // 배치 시도
        if (board.TryPlace(tile.Coord, UnitObject, slot.kind, placeYOffset, out string reason)) 
        {
            //조건이 충족되어 배치 성공 시
            SetUnit(UnitObject, tile, slot);       // 배치 성공 → 상태 갱신 및 훅 호출
        }
        else
        {
            //조건에 맞지 않아 배치 실패 시
            SetFailedUnit(UnitObject, tile, reason); // 배치 실패 → 생성한 오브젝트 제거
            SetFailMessage(tile, reason);
        }
    }

    private bool CanPlaceUnit(Tile tile, Placeable slot)
    {
        return board.CanPlace(tile.Coord, slot.kind, out string reason);
    }
    private void SetFailMessage(Tile tile, string reason)
    {
        _status = $"{tile.Coord} {reason}";
    }

    private GameObject CreateUnit(Placeable slot)//조건에 맞을경우 배치 오브젝트를 생성.
    {
        CheckPrefab(slot);             // 프리팹이 없으면 예외
        //테스트용 코드
        if(slot.kind == OccupantKind.Resource)
        {
            if(resourcesManager.CheckResources(slot.prefab.GetComponent<ProductionFacility>().BasicValue.ConstructProduct))
            return pool.Rent(slot.prefab.GetComponent<ProductionFacility>().ProductionType);// 배치할 오브젝트 생성
        }
        //끝
        return resolver.Instantiate(slot.prefab);// 배치할 오브젝트 생성
    }
    
    private void SetUnit(GameObject unit, Tile tile, Placeable slot)//배치 직후 커버리지 등록 및 훅 호출
    {
        _selectedTile = tile;                         // 배치 직후 선택 상태 유지
        _placed.Add(unit);                            // 배치된 오브젝트 목록에 추가
        Placed?.Invoke(slot.kind, unit, tile);       // 배치 이벤트 호출
        //테스트 코드
        OnPlaced?.Invoke();
        //끝
        RegisterUnit(unit, tile, slot);              // 배치된 오브젝트의 사거리 정보를 받아서 사거리 표시 및 커버리지 등록
    }

    private void SetFailedUnit(GameObject go, Tile tile, string reason)//배치 실패 시 생성한 오브젝트 제거
    {
        if (go != null) Destroy(go);
        _status = $"{tile.Coord} {reason}";
    }

    private bool TryGetSlot(out Placeable entry) // 현재 팔레트 인덱스에 해당하는 배치할 프리팹 정보를 반환.
    {
        List<Placeable> pal = Palette();    // 배치할 프리팹 목록
        if (_paletteIndex < 0 || _paletteIndex >= pal.Count)
        {
            entry = null;
            return false;
        }
        entry = pal[_paletteIndex];       // 배치할 프리팹 정보
        return true;
    }

    private void BindBoard(GameObject unit)
    {
        IPlaceAble placeable = unit.GetComponent<IPlaceAble>();
        if (placeable != null)
        {
            placeable.SetBoard(board);
        }
    }

    //유닛의 사거리 정보를 받아서 사거리 표시 (현재는 도입X)
    private void RegisterUnit(GameObject unit, Tile tile, Placeable slot)
    {
        _ranges[unit] = slot.attackRange;
        RegisterCover(unit, tile, slot.kind, slot.attackRange);
    }
    private static void CheckPrefab(Placeable entry)
    {
        if (entry.prefab == null)
        {
            throw new MissingReferenceException($"{entry.label} 프리팹이 없습니다.");
        }
    }

    private void RegisterCover(GameObject unit, Tile tile, OccupantKind kind, int range)
    {
        if (board == null || unit == null || tile == null) return;
        if (kind == OccupantKind.Building) return;

        board.SetRangeCover(unit, tile.Coord, Mathf.Max(0, range));
    }

    public int UnitRange(GameObject go, OccupantKind kind)
    {
        if (kind == OccupantKind.Building)
        {
            return -1;
        }

        return _ranges.TryGetValue(go, out int range) ? range : -1;
    }

    private int PreviewRange(OccupantKind kind)
    {
        List<Placeable> pal = Palette();
        return kind == OccupantKind.Building ? -1 : Mathf.Max(0, pal[_paletteIndex].attackRange);
    }

    // 배치된 유닛을 집어 든다: 타일 점유 정보를 전부 떼고(사거리 커버 해제 포함) 프리뷰 상태로 전환. Destroy 안 함.
    private void PickUpUnit(Tile tile)
    {
        OccupantKind kind = tile.State.Occupant;      // 제거 전에 종류를 읽어 둔다(ClearOccupant가 None으로 바꿈).
        GameObject go = board.RemoveUnit(tile.Coord);
        if (go == null) return;

        _held = go;
        _heldFrom = tile;
        _heldKind = kind;
        _heldRange = _ranges.TryGetValue(go, out int range) ? range : 0;
        _selectedTile = tile;
        _status = HeldInfo; // 패널 Status가 이 문자열을 그대로 표시(집은 유닛 정보 노출).
    }

    // 집은 유닛을 tile에 내려놓는다. 배치 규칙 통과 시 재배치(정보 복원), 아니면 차단 + 사유 표시(집은 채 유지).
    private void Drop(Tile tile)
    {
        if (tile == null)
        {
            _status = "타일 없음";
            return;
        }

        if (!board.CanPlace(tile.Coord, _heldKind, out string reason))
        {
            _status = $"{tile.Coord} {reason}";
            return;
        }

        board.TryPlace(tile.Coord, _held, _heldKind, placeYOffset, out _);
        RegisterCover(_held, tile, _heldKind, _heldRange);
        _selectedTile = tile;
        _status = $"{tile.Coord} 이동";
        ClearHeld();
    }

    // 집은 유닛 프리뷰가 목표 타일 윗면을 따라가게 한다(그리드 밖이면 가장자리 타일로 클램프).
    private void FollowHeld()
    {
        if (_held == null) return;

        Tile tile = HeldTarget();
        if (tile != null) _held.transform.position = tile.WorldTop + Vector3.up * placeYOffset;
    }

    // 집은 유닛의 목표 타일: 커서가 그리드 밖이면 가장 가까운 가장자리 타일로 클램프.
    private Tile HeldTarget()
    {
        if (_cam == null || Mouse.current == null) return null;
        Ray ray = _cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        return board.NearestCellFromRay(ray);
    }

    private void ClearHeld()
    {
        _heldKind = OccupantKind.None;
        _heldRange = 0;
        _held = null;
        _heldFrom = null;
        _heldRange = 0;
    }

    private void RemoveAt(Tile tile)
    {
        GameObject go = board.RemoveUnit(tile.Coord);
        if (go == null) return;
        _placed.Remove(go);
        _ranges.Remove(go);
        //테스트용 코드
        if(go.GetComponent<ProductionFacility>() != null)
        {
            go.GetComponent<ProductionFacility>().Release();
        }
        else
        {
            Destroy(go);
        }
        //끝
        if (_selectedTile == tile)
        {
            _selectedTile = null;
        }
        _status = $"{tile.Coord} 제거";
    }

    public void ClearAllPlacedUnit()
    {
        if (_held != null) // 집은 채 전체 제거 시 보드에 없는 프리뷰 유닛도 정리(고아 방지).
        {
            Destroy(_held);
            ClearHeld();
        }
        foreach (Tile tile in board.Cells.Values)
        {
            if (tile.OccupantObject == null) continue;
            GameObject go = board.RemoveUnit(tile.Coord);
            if (go != null) Destroy(go);
        }
        _placed.Clear();
        _ranges.Clear();
        _selectedTile = null;
        _status = "배치 전부 제거";
    }

    private Tile PickCellUnderPointer() //마우스 포인터 아래 타일을 반환. 없으면 null
    {
        if (_cam == null || Mouse.current == null) return null;
        Ray ray = _cam.ScreenPointToRay(Mouse.current.position.ReadValue());

        // 타일별 실제 윗면 높이로 정확 피킹(High 등 높이가 달라도 윗면 클릭이 맞는다).
        return board.CellFromRay(ray);
    }

    public void SetUnit(int index)
    {
        List<Placeable> items = Palette();
        if (index < 0 || index >= items.Count)
        {
            return;
        }

        _paletteIndex = index;
        _mode = PlaceMode.Place;
    }

    //테스트 코드
    public void SetUnit(string label)
    {
        List<Placeable> items = Palette();
        foreach(var item in items)
        {
            if(item.label == label)
            {
                _paletteIndex = items.IndexOf(item);
            }
        }

        _mode = PlaceMode.Place;
    }
    //끝

    public void SetRemove()
    {
        _mode = PlaceMode.Remove;
    }

    public void ClearMode()
    {
        _mode = PlaceMode.Off;
    }

    public void SetBlock(bool value)
    {
        _inputBlocked = value;
    }

    public bool CheckCanBuild(string label)
    {
        var slot = palette.Find(x => x.label == label);
        if (slot == null || slot.prefab == null) return false;

        switch (slot.kind)
        {
            case OccupantKind.None:
                return true;
            case OccupantKind.MeleeHero:
                var melee = slot.prefab.GetComponent<Hero>();
                if (melee == null) return true;

                if (citizenManager.CheckCanUseCitizen())
                    return true;
                else
                {
                    Debug.Log("집 호출");
                    return false;
                }
            case OccupantKind.RangedHero:
                var range = slot.prefab.GetComponent<Hero>();
                if (range == null) return true;

                if (citizenManager.CheckCanUseCitizen())
                    return true;
                else
                {
                    Debug.Log("집 호출");
                    return false;
                }
            case OccupantKind.Building:
                var house = slot.prefab.GetComponent<House>();
                if(house == null) return true;

                if (resourcesManager.CheckResources(house.Resources))
                    return true;
                else
                {
                    Debug.Log("집 호출");
                    return false;
                }
            case OccupantKind.Resource:
                var facility = slot.prefab.GetComponent<ProductionFacility>();
                if (facility == null) return true;

                if (resourcesManager == null)
                    Debug.Log("자원 관리자 없음");
                if (resourcesManager.CheckResources(facility.BasicValue.ConstructProduct))
                    return true;
                else
                {
                    return false;
                }
            default: return true;
        }
    }
}
