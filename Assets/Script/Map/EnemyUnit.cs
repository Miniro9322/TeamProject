using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 주어진 waypoint(타일 윗면 좌표)를 순서대로 따라간다.
/// waypoint가 4방향 인접 칸이라 이동이 자연히 상하좌우로 보인다.
/// 항상 타일 윗면 + yOffset 높이를 유지해 타일 위에 올라와 있게 한다.
/// </summary>
public class EnemyUnit : MonoBehaviour
{
    public float speed = 2f;
    public float yOffset = 0.5f;
    public bool destroyOnArrive = true;

    private readonly List<Vector3> _path = new();
    private int _index;
    private bool _active;

    /// <summary>경로를 주입하고 시작 지점에 배치한다.</summary>
    public void SetPath(IReadOnlyList<Vector3> worldPath, float moveSpeed, float surfaceOffset)
    {
        speed = moveSpeed;
        yOffset = surfaceOffset;

        _path.Clear();
        if (worldPath != null)
            foreach (Vector3 p in worldPath) _path.Add(p + Vector3.up * yOffset);

        _index = 0;
        _active = _path.Count > 0;
        if (_active) transform.position = _path[0];
    }

    private void Update()
    {
        if (!_active) return;

        Vector3 target = _path[_index];
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        // 진행 방향 바라보기(수평 성분만).
        Vector3 flat = target - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), 12f * Time.deltaTime);

        if (Vector3.SqrMagnitude(transform.position - target) > 0.0004f) return;

        // 다음 waypoint로.
        _index++;
        if (_index < _path.Count) return;

        _active = false;
        if (destroyOnArrive) Destroy(gameObject);
    }
}
