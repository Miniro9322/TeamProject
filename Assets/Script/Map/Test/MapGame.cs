using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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

    private Camera _cam;
    private PlaceMode _mode = PlaceMode.Off;
    private int _paletteIndex;

    private readonly List<GameObject> _placed = new();
    private readonly Dictionary<GameObject, int> _ranges = new();

    private Tile _selectedTile;
    private bool _inputBlocked;
    private string _status = "";

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
        HandlePlacementClick();
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

    private void HandlePlacementClick()
    {
        if (_inputBlocked)
        {
            return;
        }
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;

        Tile tile = PickCellUnderPointer();
        if (tile == null) return;

        switch (_mode)
        {
            case PlaceMode.Remove: RemoveAt(tile); break;
            case PlaceMode.Place: PlaceUnit(tile); break;
            default: SelectTile(tile); break;
        }
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
        return Instantiate(slot.prefab);// 배치할 오브젝트 생성
    }

    private void SetUnit(GameObject unit, Tile tile, Placeable slot)//배치 직후 커버리지 등록 및 훅 호출
    {
        _selectedTile = tile;                         // 배치 직후 선택 상태 유지
        _placed.Add(unit);                            // 배치된 오브젝트 목록에 추가
        Placed?.Invoke(slot.kind, unit, tile);       //배치 이벤트 호출
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

    private void RemoveAt(Tile tile)
    {
        GameObject go = board.Vacate(tile.Coord);
        if (go == null) return;
        _placed.Remove(go);
        _ranges.Remove(go);
        Destroy(go);
        if (_selectedTile == tile)
        {
            _selectedTile = null;
        }
        _status = $"{tile.Coord} 제거";
    }

    public void ClearPlaced()
    {
        foreach (Tile tile in board.Cells.Values)
        {
            if (tile.OccupantObject == null) continue;
            GameObject go = board.Vacate(tile.Coord);
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

}
