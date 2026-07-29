using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class TilePainter : MonoBehaviour
{
    //디버그용 타일 색상 변경
    public MapBoard board; // 주입(자동탐색 금지)

    [Header("Colors")]
    public Color pathColor = new(0.22f, 0.85f, 0.54f);
    public Color okColor = new(0.21f, 0.77f, 0.41f);
    public Color denyColor = new(0.85f, 0.29f, 0.27f);
    [Tooltip("배치 프리뷰/호버 시 유닛 사거리 타일 색.")]
    public Color rangeColor = new(0.30f, 0.60f, 1f);
    [Tooltip("적이 올라온 타일 색.")]
    public Color enemyColor = new(0.90f, 0.25f, 0.20f);
    [Tooltip("아군+적이 겹친(저지 중) 타일 색.")]
    public Color blockColor = new(0.95f, 0.55f, 0.10f);

    [Header("칠하기")]
    [Tooltip("색 판에 쓸 셰이더. 비워두면 URP/Unlit을 알아서 잡는다.")]
    public Shader tintShader;
    [Range(0f, 1f)]
    [Tooltip("색 판의 진하기. 낮출수록 타일 무늬가 잘 보인다.")]
    public float tintAlpha = 0.45f;
    [Tooltip("타일 윗면에서 이만큼 띄워 그린다.")]
    public float lift = 0.02f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private readonly Dictionary<Color, Material> _tints = new();      // 색마다 만들어 둔 판 머티리얼
    private readonly Dictionary<Tile, GameObject> _marks = new();     // 칠해진 타일 → 그 위에 띄운 판
    private readonly Stack<GameObject> _pool = new();                 // 다 쓴 판(매 프레임 다시 쓴다)
    private Mesh _quad;
    private bool _shaderMissing;

    public void SetColor(Vector2Int coord, Color color)
    {
        if (board.TryGetCell(coord, out Tile tile)) Paint(tile, color);
    }

    public void ClearColor(Vector2Int coord)
    {
        if (board.TryGetCell(coord, out Tile tile)) Restore(tile);
    }

    // 타일을 직접 받는 경로 — 좌표는 모듈 로컬이라 보드 역조회가 모듈을 특정 못 하므로,
    // 어느 모듈 타일이든 색칠하려면 이쪽을 쓴다.
    public void SetColor(Tile tile, Color color)
    {
        Paint(tile, color);
    }

    public void ClearColor(Tile tile)
    {
        Restore(tile);
    }

    // 타일 머티리얼은 건드리지 않고 윗면 위에 색 판을 한 장 띄운다.
    // 타일 셰이더에 색 프로퍼티가 없는 것도 있어서, 타일 자체를 물들이는 방식은 쓸 수 없다.
    private void Paint(Tile tile, Color color)
    {
        if (tile == null || !Paintable(tile))
        {
            return;
        }

        Material tint = Tint(color);
        if (tint == null)
        {
            return;
        }

        if (!_marks.TryGetValue(tile, out GameObject mark))
        {
            mark = Take();
            _marks[tile] = mark;
        }

        float size = tile.Board != null ? tile.Board.CellSize : 1f;
        mark.transform.SetPositionAndRotation(
            tile.WorldTop + Vector3.up * lift,
            Quaternion.Euler(90f, 0f, 0f));
        mark.transform.localScale = new Vector3(size, size, 1f);
        mark.GetComponent<MeshRenderer>().sharedMaterial = tint;
        mark.SetActive(true);
    }

    private void Restore(Tile tile)
    {
        if (tile == null || !_marks.TryGetValue(tile, out GameObject mark))
        {
            return;
        }

        _marks.Remove(tile);
        mark.SetActive(false);
        _pool.Push(mark);
    }

    // 칠해도 되는 타일인가. 어느 칠하기 경로로 와도 여기서 걸린다.
    // 장식 타일은 외곽 줄이든 생산·전투를 가르는 안쪽 벽이든 칠하지 않는다 —
    // 덮는 칸에 걸치면 그 자리가 빈 채로 남아, 무엇 때문에 못 놓는지가 그대로 보인다.
    private static bool Paintable(Tile tile)
    {
        if (tile.IsSpecial)
        {
            return false;
        }

        if (tile.Board == null)
        {
            return true;   // 보드 도장 전(에디터 도구)에는 맵 밖인지 판정할 근거가 없다
        }

        RectInt rect = tile.Board.PlayRect;
        return rect.width <= 0 || rect.Contains(tile.Coord);
    }

    // 판 하나를 꺼낸다. 타일의 자식으로 두면 MapBoard가 윗면 높이를 이 판까지 포함해 재므로 painter 아래에 붙인다.
    private GameObject Take()
    {
        if (_pool.Count > 0)
        {
            return _pool.Pop();
        }

        GameObject mark = new("TileMark");
        mark.hideFlags = HideFlags.DontSave;
        mark.transform.SetParent(transform, false);
        mark.AddComponent<MeshFilter>().sharedMesh = Quad();

        MeshRenderer renderer = mark.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        return mark;
    }

    // 한 칸 크기(1×1) 판 메시. Euler(90,0,0)으로 세우면 윗면을 보게 되도록 -Z를 향해 만든다.
    private Mesh Quad()
    {
        if (_quad != null)
        {
            return _quad;
        }

        _quad = new Mesh { name = "TileMarkQuad", hideFlags = HideFlags.DontSave };
        _quad.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3( 0.5f, -0.5f, 0f),
            new Vector3(-0.5f,  0.5f, 0f),
            new Vector3( 0.5f,  0.5f, 0f),
        };
        _quad.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
        _quad.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
        _quad.triangles = new[] { 0, 1, 2, 2, 1, 3 };

        return _quad;
    }

    // 덧칠에 쓸 셰이더. 인스펙터가 비어 있으면 직접 잡는다(씬 배선이 없어도 돌아야 한다).
    // 경고는 한 번만 — 매 프레임 타일마다 불리는 자리라 그냥 찍으면 로그가 넘친다.
    private Shader TintShader()
    {
        if (tintShader != null)
        {
            return tintShader;
        }

        if (_shaderMissing)
        {
            return null;
        }

        tintShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (tintShader == null)
        {
            tintShader = Shader.Find("Universal Render Pipeline/Lit");
        }

        if (tintShader == null)
        {
            _shaderMissing = true;
            Debug.LogWarning("[TilePainter] 덧칠용 셰이더를 찾지 못했습니다. tintShader에 색 있는 셰이더를 직접 넣으세요.", this);
        }

        return tintShader;
    }

    // 색마다 반투명 머티리얼을 한 번만 만들어 재사용한다.
    private Material Tint(Color color)
    {
        if (_tints.TryGetValue(color, out Material cached))
        {
            return cached;
        }

        Shader shader = TintShader();
        if (shader == null)
        {
            return null;
        }

        Material mat = new(shader) { hideFlags = HideFlags.DontSave };
        mat.SetColor(BaseColorId, new Color(color.r, color.g, color.b, tintAlpha));

        // URP 투명 설정. 깊이를 쓰지 않아 타일 윗면과 겹쳐도 z 다툼이 없다.
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.SetFloat("_AlphaClip", 0f);
        mat.SetFloat("_Cull", (float)CullMode.Off);   // 판이 뒤집혀도 보이게
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.renderQueue = (int)RenderQueue.Transparent;

        _tints[color] = mat;
        return mat;
    }

    private void OnDestroy()
    {
        foreach (Material tint in _tints.Values)
        {
            Destroy(tint);
        }
        _tints.Clear();

        if (_quad != null)
        {
            Destroy(_quad);
            _quad = null;
        }
    }

    // ---- 에디터 디버그: 지형 한눈에 보기(우클릭 실행) ----
    // 노랑=스폰, 파랑=본진(Core), 빨강=High(벽), 초록=Ground(통행), 회색=Empty(벽).
    // 장식(Special)은 여기서도 칠하지 않아 빈자리로 남는다.

    [ContextMenu("Tint Terrain")]
    public void TintTerrain()
    {
        if (board.Cells.Count == 0) board.Build();

        foreach (Tile t in board.Cells.Values)
        {
            Color c = t.isEnemySpawn ? new Color(0.95f, 0.9f, 0.2f)
                : t.Terrain switch
                {
                    TerrainType.Core   => new Color(0.2f, 0.5f, 1f),
                    TerrainType.High   => new Color(0.9f, 0.3f, 0.2f),
                    TerrainType.Ground => new Color(0.3f, 0.8f, 0.4f),
                    _                  => new Color(0.5f, 0.5f, 0.5f),
                };
            Paint(t, c);
        }
    }

    [ContextMenu("Clear Tint")]
    public void ClearTint()
    {
        foreach (Tile t in board.Cells.Values) Restore(t);
    }
}
