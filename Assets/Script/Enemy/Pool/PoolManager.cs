using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;
using VContainer.Unity;


public class PoolManager : MonoBehaviour
{
    private IObjectResolver _resolver;
    [Inject] public void Construct(IObjectResolver resolver) => _resolver = resolver;

    private static PoolManager instance;
    public static PoolManager Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("PoolManager");
                instance = go.AddComponent<PoolManager>();
            }
            return instance;
        }
    }

    // 프리팹 → 그 프리팹 전용 풀
    private readonly Dictionary<GameObject, ObjectPool<GameObject>> pools = new();
    // 프리팹 → 비활성 인스턴스가 모이는 컨테이너(reparent 회수 기준점)
    private readonly Dictionary<GameObject, Transform> roots = new();

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    private ObjectPool<GameObject> GetPool(GameObject prefab)
    {
        if (pools.TryGetValue(prefab, out var pool)) return pool;

        var root = new GameObject($"Pool_{prefab.name}").transform;
        root.SetParent(transform);
        roots[prefab] = root;

        pool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                // resolver가 있으면 그걸로 생성 → 스폰된 적의 [Inject]가 채워진다. 없으면 폴백.
                var go = _resolver != null
                    ? _resolver.Instantiate(prefab, root)
                    : Instantiate(prefab, root);
                var po = go.GetComponent<PooledObject>();
                if (po == null) po = go.AddComponent<PooledObject>();
                po.Init(prefab, this); // 자기 풀을 넘겨 회수 시 쓰게(AddComponent는 주입 안 되므로 직접 전달)
                go.SetActive(false);
                return go;
            },
            actionOnGet: null,                         // 활성화/위치 세팅은 Spawn에서
            actionOnRelease: go =>
            {
                if (go == null) return;
                go.transform.SetParent(root);          // 부모(적 등)에 붙었던 이펙트도 풀 루트로 회수
                go.SetActive(false);
            },
            actionOnDestroy: go => { if (go != null) Destroy(go); },
            collectionCheck: true,                     // 중복 Release 감지(에디터 안전망)
            defaultCapacity: 8,
            maxSize: 256
        );
        pools[prefab] = pool;
        return pool;
    }

    public GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent = null)
    {
        if (prefab == null) return null;

        var pool = GetPool(prefab);
        var go = pool.Get();
        var t = go.transform;
        if (parent != null) t.SetParent(parent);
        t.SetPositionAndRotation(pos, rot);

        go.SetActive(true);
        go.GetComponent<PooledObject>().OnSpawned();   // 활성화 후 파티클 리셋/상태 초기화
        return go;
    }

    public void Despawn(GameObject go)
    {
        if (go == null) return;

        var po = go.GetComponent<PooledObject>();
        if (po == null) { Destroy(go); return; }       // 풀 출신이 아니면 그냥 파괴(안전망)
        if (po.IsReleased) return;                     // 중복 Despawn 가드

        po.MarkReleased();
        GetPool(po.SourcePrefab).Release(go);
    }
    public void Despawn(GameObject go, float delay,bool scaleCheck = true)
    {
        if (go == null) return;
        if (delay <= 0f) { Despawn(go); return; }

        var po = go.GetComponent<PooledObject>();
        if (po == null) { Destroy(go, delay); return; }
        po.ScheduleDespawn(delay,scaleCheck);

        
    }
}