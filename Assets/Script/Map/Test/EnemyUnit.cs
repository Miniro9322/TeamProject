using System.Collections.Generic;
using UnityEngine;

//적 관련 임시 코드 파일
public class EnemyUnit : MonoBehaviour
{
    public float speed = 2f;
    public float yOffset = 0.5f;
    public bool destroyOnArrive = true;
    public MapBoard board;

    //적이 이동할 경로 List에 해당 타일을 넣고 경로를 SetPath로 주면, EnemyUnit이 자동으로 이동하며 도착 시 파괴된다.
    private readonly List<Vector3> _path = new();
    private readonly List<Tile> _pathTiles = new(); // _path와 같은 순서·길이. 있으면 칸 판정을 인덱스로 함
    private int _index;
    private bool _active;

    //월드 경로 주면 칸 판정은 WorldToCell 폴백. pathTiles를 함께 주면 칸 판정을 타일 인덱스로 한다(권장).
    public void SetPath(IReadOnlyList<Vector3> worldPath, float moveSpeed, float surfaceOffset, IReadOnlyList<Tile> pathTiles = null)
    {
        speed = moveSpeed;
        yOffset = surfaceOffset;

        _path.Clear();
        _pathTiles.Clear();
        if (worldPath != null)
            foreach (Vector3 p in worldPath) _path.Add(p + Vector3.up * yOffset);
        if (pathTiles != null)
            _pathTiles.AddRange(pathTiles);

        _index = 0;
        _active = _path.Count > 0;
        if (_active)
        {
            transform.position = _path[0];
            ReportCell(0); // 시작 칸(스폰) 등록
        }
    }

    private void Update()
    {
        if (!_active) return;

        // 저지 확인: 직전에 도달해 등록된 칸 기준. 저지 중이면 전진을 멈춘다(아군과 같은 타일에 겹쳐 정지).
        if (board != null && board.IsBlocked(gameObject)) return;

        Vector3 target = _path[_index];
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        // 진행 방향 바라보기
        Vector3 flat = target - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), 12f * Time.deltaTime);

        if (Vector3.SqrMagnitude(transform.position - target) > 0.0004f) return;

        // 이번 waypoint 도달 → 그 칸에 올라선 것으로 등록. 전환은 인덱스 기준으로 일어난다.
        ReportCell(_index);

        _index++;
        if (_index < _path.Count) return;

        _active = false;
        if (destroyOnArrive) Destroy(gameObject);
    }

    // 경로 타일이 있으면 인덱스로 정확히 보고
    private void ReportCell(int reachedIndex)
    {
        if (board == null) return;
        if (_pathTiles.Count > 0 && reachedIndex >= 0 && reachedIndex < _pathTiles.Count)
            board.SetEnemyCell(gameObject, _pathTiles[reachedIndex]);
        else
            board.MoveEnemy(gameObject, transform.position);
    }

    // 도착 파괴·웨이브 정리 등 어떤 경로로 사라지든 현재 칸에서 빠지도록 한 곳에서 해제한다.
    private void OnDestroy()
    {
        if (board != null) board.RemoveEnemy(gameObject);
    }
}
