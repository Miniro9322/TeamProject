using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// 배치 모드에서 커서가 가리키는 자리에 놓일 모습을 미리 세워 보여준다.
public class PlaceGhost
{
    // 타일 색 판보다 늦게 그리는 큐(큐가 거리 정렬보다 우선한다).
    private const int GhostQueue = (int)RenderQueue.Transparent + 100;

    // 반투명으로 바꿀 때 건드리는 URP 프로퍼티.
    private const string TransparentKeyword = "_SURFACE_TYPE_TRANSPARENT";
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
    private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
    private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
    private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");

    private readonly MapView view;
    private readonly float yOffset;
    private readonly float alpha;
    private readonly Dictionary<GameObject, GameObject> ghosts = new();   // 프리팹 → 만들어 둔 미리보기
    private readonly Dictionary<Material, Material> fades = new();        // 원본 머티리얼 → 반투명 사본
    private GameObject shownGhost;

    public PlaceGhost(MapView view, float yOffset, float alpha)
    {
        this.view = view;
        this.yOffset = yOffset;
        this.alpha = alpha;
    }

    // 미리보기를 세울지 감출지 고른다.
    public void FollowCursor()
    {
        PlacementArea area = GhostArea();
        GameObject prefab = view.PlacingPrefab;

        if (area == null || prefab == null)
        {
            HideGhost();
            return;
        }

        ShowGhost(prefab, area);
    }

    // 미리보기가 놓일 자리를 낸다. 배치 모드가 아니면 놓일 자리가 없다.
    private PlacementArea GhostArea()
    {
        if (view.InputBlocked || !view.IsPlacing)
        {
            return null;
        }

        return view.HoverArea;
    }

    // 미리보기를 자리에 세운다.
    private void ShowGhost(GameObject prefab, PlacementArea area)
    {
        MakeGhost(prefab);
        GameObject ghost = ghosts[prefab];
        ghost.transform.position = GhostPosition(area);
        SwapGhost(ghost);
    }

    // 미리보기가 설 지점을 낸다. 높이는 확정 배치와 같은 계산을 쓴다.
    private Vector3 GhostPosition(PlacementArea area)
    {
        Vector3 position = area.Center;
        position.y = AreaPlace.TopY(area, view.PlacingKind) + yOffset;
        return position;
    }

    // 장부에 빠진 미리보기를 채운다.
    private void MakeGhost(GameObject prefab)
    {
        if (ghosts.TryGetValue(prefab, out GameObject ghost) && ghost != null)
        {
            return;
        }

        ghosts[prefab] = BuildGhost(prefab);
    }

    // 겉모습만 남긴 복제본을 만든다. 꺼둔 껍데기 안에서 복제해야 스크립트가 깨어나지 않는다.
    private GameObject BuildGhost(GameObject prefab)
    {
        // hideFlags(DontSave)를 붙이면 씬 언로드에서 빠져 플레이 종료 후에도 남는다.
        GameObject ghost = new(prefab.name + " (Ghost)");
        ghost.SetActive(false);

        GameObject model = Object.Instantiate(prefab, ghost.transform);
        model.transform.localPosition = Vector3.zero;   // 확정 배치가 유닛 루트를 자리에 딱 놓는 것과 같게
        StripScripts(model);
        FadeGhost(model);

        return ghost;
    }

    // 복제본에서 유닛 스크립트를 떼어낸다(Animator는 남아 컨트롤러 기본 상태인 idle을 재생한다).
    private static void StripScripts(GameObject model)
    {
        foreach (MonoBehaviour script in model.GetComponentsInChildren<MonoBehaviour>(true))
        {
            Object.DestroyImmediate(script);
        }
    }

    // 복제본의 머티리얼을 반투명 사본으로 바꾼다.
    private void FadeGhost(GameObject model)
    {
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            for (int index = 0; index < materials.Length; index++)
            {
                MakeFade(materials[index]);
                materials[index] = fades[materials[index]];
            }

            renderer.sharedMaterials = materials;
        }
    }

    // 장부에 빠진 반투명 사본을 채운다. 못 바꾸는 머티리얼은 원본을 그대로 쓴다.
    private void MakeFade(Material source)
    {
        if (fades.ContainsKey(source))
        {
            return;
        }

        if (!CanFade(source))
        {
            Debug.LogWarning($"[PlaceGhost] {source.shader.name}에 URP 투명 프로퍼티가 없어 미리보기가 불투명하게 뜹니다.");
            fades[source] = source;
            return;
        }

        fades[source] = BuildFade(source);
    }

    // 반투명으로 바꿀 수 있는 머티리얼인지 본다. 없는 프로퍼티에 쓰면 유니티가 조용히 무시한다.
    private static bool CanFade(Material source)
    {
        return source.HasProperty(BaseColorId)
            && source.HasProperty(SurfaceId)
            && source.HasProperty(SrcBlendId)
            && source.HasProperty(DstBlendId)
            && source.HasProperty(ZWriteId)
            && new LocalKeyword(source.shader, TransparentKeyword).isValid;
    }

    // 원본을 베낀 반투명 머티리얼을 만든다. 설정은 URP 투명 그대로다.
    private Material BuildFade(Material source)
    {
        Material fade = new(source);

        Color color = fade.GetColor(BaseColorId);
        color.a = alpha;
        fade.SetColor(BaseColorId, color);

        fade.SetFloat(SurfaceId, 1f);
        fade.SetFloat(SrcBlendId, (float)BlendMode.SrcAlpha);
        fade.SetFloat(DstBlendId, (float)BlendMode.OneMinusSrcAlpha);
        fade.SetFloat(ZWriteId, 0f);   // 깊이를 쓰면 렌더러 정렬이 위치에 따라 뒤바뀌어 부분마다 진하기가 달라진다
        fade.SetKeyword(new LocalKeyword(fade.shader, TransparentKeyword), true);
        fade.renderQueue = GhostQueue;

        return fade;
    }

    // 보이는 미리보기를 갈아 끼운다.
    private void SwapGhost(GameObject ghost)
    {
        if (shownGhost == ghost)
        {
            return;
        }

        HideGhost();
        ghost.SetActive(true);
        shownGhost = ghost;
    }

    // 보이는 미리보기를 감춘다.
    private void HideGhost()
    {
        if (shownGhost == null)
        {
            return;
        }

        shownGhost.SetActive(false);
        shownGhost = null;
    }
}
