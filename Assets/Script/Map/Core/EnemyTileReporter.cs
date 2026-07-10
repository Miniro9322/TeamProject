using UnityEngine;

public class EnemyTileReporter : MonoBehaviour
{
    public MapBoard board;

    //적이 경로상 지상 유닛으로 인해 저지 당했는지 여부.
    public bool IsBlocked { get; private set; }

    // 매 프레임 자동 보고 → board만 주입돼 있으면 타일의 "내 위 적" 목록이 갱신된다.
    private void Update() => Report();

    /// <summary>지금 위치를 보드에 보고하고 저지 여부를 갱신해 돌려준다. 이동 직후 호출하면 같은 프레임에 반영된다.</summary>
    public bool ReportAndCheckBlocked()
    {
        Report();
        return IsBlocked;
    }

    private void Report()
    {
        if (board == null) { IsBlocked = false; return; }
        board.MoveEnemy(gameObject, transform.position);
        IsBlocked = board.IsBlocked(gameObject);
    }

    // 비활성/파괴 어느 쪽이든(활성 오브젝트 파괴 시 OnDisable이 먼저 불린다) 현재 칸에서 빠지도록 한 곳에서 정리.
    // 다시 활성화되면 다음 Update의 보고에서 현재 칸으로 재등록된다.
    private void OnDisable()
    {
        IsBlocked = false;
        if (board != null) board.RemoveEnemy(gameObject);
    }
}
