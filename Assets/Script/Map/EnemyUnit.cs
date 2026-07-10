using System.Collections.Generic;
using UnityEngine;

//적 관련 임시 코드 파일
public class EnemyUnit : MonoBehaviour
{
    public float speed = 2f;
    public float yOffset = 0.5f;
    public bool destroyOnArrive = true;

    private readonly List<Vector3> _path = new();
    private int _index;
    private bool _active;

  
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
