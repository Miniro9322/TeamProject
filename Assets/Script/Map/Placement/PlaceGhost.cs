using System.Collections.Generic;
using UnityEngine;

// 배치 모드에서 커서가 가리키는 자리에 놓일 모습을 미리 세워 보여준다.
public class PlaceGhost
{
    private readonly MapView view;
    private readonly float yOffset;
    private readonly Dictionary<GameObject, GameObject> ghosts = new();   // 프리팹 → 만들어 둔 미리보기
    private GameObject shownGhost;

    public PlaceGhost(MapView view, float yOffset)
    {
        this.view = view;
        this.yOffset = yOffset;
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
        position.y = AreaPlace.TopY(area) + yOffset;
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
    private static GameObject BuildGhost(GameObject prefab)
    {
        GameObject ghost = new(prefab.name + " (Ghost)");
        ghost.hideFlags = HideFlags.DontSave;
        ghost.SetActive(false);

        GameObject model = Object.Instantiate(prefab, ghost.transform);
        model.transform.localPosition = Vector3.zero;   // 확정 배치가 유닛 루트를 자리에 딱 놓는 것과 같게
        StripScripts(model);

        return ghost;
    }

    // 복제본에서 스크립트를 떼어낸다. 끄는 것으로는 Awake·Start를 막을 수 없고,
    // 주입이 없는 껍데기는 House.Start·Hero.Start에서 그대로 터진다.
    // Animator는 스크립트가 아니라 남는다 — 컨트롤러 기본 상태가 idle이라 껍데기가 저절로 idle을 재생한다.
    private static void StripScripts(GameObject model)
    {
        foreach (MonoBehaviour script in model.GetComponentsInChildren<MonoBehaviour>(true))
        {
            Object.DestroyImmediate(script);
        }
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
