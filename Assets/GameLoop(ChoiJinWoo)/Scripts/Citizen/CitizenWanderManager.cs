using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using VContainer;
using Random = UnityEngine.Random;
public class CitizenWanderManager : MonoBehaviour
{
    [SerializeField] private GameObject[] citizenPrefabs;
    [SerializeField] private float wanderRadius = 6f;
    [SerializeField] private int visibleCap = 60;
    [SerializeField] private int estimatedMaxCitizenCeiling = 300;

    [Header("인사")]
    [SerializeField] private float greetCheckInterval = 1.5f;
    [SerializeField] private float greetRadius = 2.5f;
    [SerializeField] private float greetChance = 0.15f;
    [SerializeField] private float greetResponseDelay = 0.6f;
    [SerializeField] private float greetDuration = 2.5f;
    [SerializeField] private string greetAnimTrigger = "Greet";

    [Header("단체 대화")]
    [SerializeField] private float groupCheckInterval = 6f;
    [SerializeField] private float groupChance = 0.2f;
    [SerializeField] private int groupMinSize = 2;
    [SerializeField] private int groupMaxSize = 3;
    [SerializeField] private float groupGatherRadius = 6f;
    [SerializeField] private float groupSpotSpacing = 0.8f;
    [SerializeField] private float groupJoinTimeout = 8f;
    [SerializeField] private float groupChatDuration = 4f;
    [SerializeField] private string groupChatAnimTrigger = "Chat";

    private class GroupSession
    {
        public List<GameObject> Members;
        public int ArrivedCount;
        public bool ChatStarted;
    }

    private Transform hubPoint;
    private Transform[] homePoints;
    private Vector3 hubGroundPosition;

    private CitizenManager citizenManager;
    private GameManager gameManager;
    private PoolManager poolManager;

    private readonly List<GameObject> activeCitizens = new();
    private bool isNight;
    private CancellationTokenSource lifetimeCts;

    [Inject]
    private void Construct(CitizenManager citizenManager, GameManager gameManager, PoolManager poolManager)
    {
        this.citizenManager = citizenManager;
        this.gameManager = gameManager;
        this.poolManager = poolManager;
    }

    private void Awake()
    {
        lifetimeCts = new CancellationTokenSource();
        citizenManager.CitizenChanged += OnCitizenChanged;
        gameManager.ChangeToNight += OnNight;
        gameManager.ChangeToDay += OnDay;
    }

    private void OnCitizenChanged() => SyncVisibleCountAsync(lifetimeCts.Token).Forget();

    private void Start()
    {
        if (hubPoint == null)
        {
            Debug.LogError("CitizenWanderManager: hubPoint가 설정되지 않았습니다. GameLifeTimeScope의 citizenHubPoint를 확인하세요.", this);
            return;
        }

        if (citizenPrefabs == null || citizenPrefabs.Length == 0)
        {
            Debug.LogError("CitizenWanderManager: citizenPrefabs가 비어 있습니다. 최소 1개 이상 등록하세요.", this);
            return;
        }

        hubGroundPosition = ResolveHubGroundPosition();
        InitializeAsync(lifetimeCts.Token).Forget();
        RunGreetCheckLoop(lifetimeCts.Token).Forget();
        RunGroupChatCheckLoop(lifetimeCts.Token).Forget();
    }

    private async UniTask RunGreetCheckLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(greetCheckInterval), cancellationToken: token);
            if (!isNight) TryTriggerGreetings();
        }
    }

    private async UniTask RunGroupChatCheckLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(groupCheckInterval), cancellationToken: token);
            if (!isNight) TryStartGroupChat();
        }
    }

    private async UniTaskVoid InitializeAsync(CancellationToken token)
    {
        await WarmPoolAsync(token);
        await SyncVisibleCountAsync(token);
    }


    private Vector3 ResolveHubGroundPosition()
    {
        if (NavMesh.SamplePosition(hubPoint.position, out var hit, wanderRadius, NavMesh.AllAreas))
            return hit.position;

        Debug.LogError("CitizenWanderManager: hubPoint 주변에서 NavMesh를 찾지 못했습니다. " +
            "NavMeshSurface 베이크 범위가 Hub 위치를 덮고 있는지 확인하세요.", this);
        return hubPoint.position;
    }

    public void SetHubPoint(Transform point)
    {
        hubPoint = point;
    }

    public void SetHomePoints(Transform[] points)
    {
        homePoints = points;
    }

    private void OnDestroy()
    {
        lifetimeCts?.Cancel();
        lifetimeCts?.Dispose();
        citizenManager.CitizenChanged -= OnCitizenChanged;
        gameManager.ChangeToNight -= OnNight;
        gameManager.ChangeToDay -= OnDay;
    }

    private void TryTriggerGreetings()
    {
        float radiusSqr = greetRadius * greetRadius;

        for (int i = 0; i < activeCitizens.Count; i++)
        {
            var a = activeCitizens[i];
            var npcA = a.GetComponent<CitizenNPC>();
            if (!npcA.IsAvailableForSocial) continue;

            for (int j = i + 1; j < activeCitizens.Count; j++)
            {
                var b = activeCitizens[j];
                var npcB = b.GetComponent<CitizenNPC>();
                if (!npcB.IsAvailableForSocial) continue;

                if ((a.transform.position - b.transform.position).sqrMagnitude > radiusSqr) continue;
                if (Random.value > greetChance) continue;

                npcA.TriggerGreet(b.transform.position, 0f, greetDuration, greetAnimTrigger);
                npcB.TriggerGreet(a.transform.position, greetResponseDelay, greetDuration, greetAnimTrigger);
                break;
            }
        }
    }

    private void TryStartGroupChat()
    {
        if (Random.value > groupChance) return;

        var available = new List<GameObject>();
        foreach (var go in activeCitizens)
        {
            if (go.GetComponent<CitizenNPC>().IsAvailableForSocial)
                available.Add(go);
        }

        int groupSize = Random.Range(groupMinSize, groupMaxSize + 1);
        if (available.Count < groupSize) return;

        var center = available[Random.Range(0, available.Count)];
        Vector3 centerPos = center.transform.position;

        var candidates = new List<GameObject>(available);
        candidates.Remove(center);
        float radiusSqr = groupGatherRadius * groupGatherRadius;
        candidates.RemoveAll(go => (go.transform.position - centerPos).sqrMagnitude > radiusSqr);

        var members = new List<GameObject> { center };
        while (members.Count < groupSize && candidates.Count > 0)
        {
            int idx = Random.Range(0, candidates.Count);
            members.Add(candidates[idx]);
            candidates.RemoveAt(idx);
        }

        if (members.Count < groupMinSize) return;

        StartGroupSession(members, centerPos);
    }

    private void StartGroupSession(List<GameObject> members, Vector3 centerPos)
    {
        if (!NavMesh.SamplePosition(centerPos, out var hit, wanderRadius, NavMesh.AllAreas))
            return;

        Vector3 gatherPoint = hit.position;
        var session = new GroupSession { Members = members };

        for (int i = 0; i < members.Count; i++)
        {
            float angle = i * Mathf.PI * 2f / members.Count;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * groupSpotSpacing;
            Vector3 spot = NavMesh.SamplePosition(gatherPoint + offset, out var spotHit, groupSpotSpacing + 0.5f, NavMesh.AllAreas)
                ? spotHit.position
                : gatherPoint;

            members[i].GetComponent<CitizenNPC>()
                .JoinGroup(spot, gatherPoint, groupJoinTimeout, () => OnGroupMemberArrived(session));
        }
    }

    private void OnGroupMemberArrived(GroupSession session)
    {
        session.ArrivedCount++;
        if (session.ChatStarted || session.ArrivedCount < session.Members.Count) return;

        session.ChatStarted = true;
        foreach (var member in session.Members)
        {
            if (member != null && member.activeInHierarchy)
                member.GetComponent<CitizenNPC>().StartGroupChat(groupChatAnimTrigger);
        }

        StartCoroutine(EndGroupSessionAfter(session, groupChatDuration));
    }

    private IEnumerator EndGroupSessionAfter(GroupSession session, float delay)
    {
        yield return new WaitForSeconds(delay);

        foreach (var member in session.Members)
        {

            if (member != null && member.activeInHierarchy)
                member.GetComponent<CitizenNPC>().EndGroupChat();
        }
    }


    private const int WarmPoolBatchSize = 5;

    private async UniTask WarmPoolAsync(CancellationToken token)
    {
        int perPrefab = Mathf.CeilToInt((float)visibleCap / citizenPrefabs.Length);
        int sinceYield = 0;

        foreach (var prefab in citizenPrefabs)
        {
            var warm = new GameObject[perPrefab];
            for (int i = 0; i < perPrefab; i++)
            {
                warm[i] = poolManager.Spawn(prefab, hubGroundPosition, Quaternion.identity);
                if (++sinceYield >= WarmPoolBatchSize)
                {
                    sinceYield = 0;
                    await UniTask.Yield(token);
                }
            }

            for (int i = 0; i < perPrefab; i++)
            {
                poolManager.Despawn(warm[i]);
                if (++sinceYield >= WarmPoolBatchSize)
                {
                    sinceYield = 0;
                    await UniTask.Yield(token);
                }
            }
        }
    }

    private int CalculateTargetCount()
    {
        int available = Mathf.Max(0, citizenManager.CurrentCitizen - citizenManager.HeroUsedCitizen);
        float ratio = estimatedMaxCitizenCeiling > 0 ? (float)available / estimatedMaxCitizenCeiling : 0f;
        return Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(ratio) * visibleCap), 0, visibleCap);
    }

    private async UniTask SyncVisibleCountAsync(CancellationToken token)
    {
        if (isNight) return;

        int target = CalculateTargetCount();
        int sinceYield = 0;

        while (activeCitizens.Count < target)
        {
            SpawnOne();
            if (++sinceYield >= WarmPoolBatchSize)
            {
                sinceYield = 0;
                await UniTask.Yield(token);
            }
        }

        while (activeCitizens.Count > target)
        {
            DespawnOne(activeCitizens[^1]);
            if (++sinceYield >= WarmPoolBatchSize)
            {
                sinceYield = 0;
                await UniTask.Yield(token);
            }
        }
    }

    private void SpawnOne()
    {
        Vector3 pos = RandomPointAroundHub();
        GameObject prefab = citizenPrefabs[Random.Range(0, citizenPrefabs.Length)];
        var go = poolManager.Spawn(prefab, pos, Quaternion.identity);
        go.GetComponent<CitizenNPC>().StartWandering(hubGroundPosition, wanderRadius);
        activeCitizens.Add(go);
    }

    private void DespawnOne(GameObject go)
    {
        activeCitizens.Remove(go);
        poolManager.Despawn(go);
    }

    private Vector3 RandomPointAroundHub()
    {
        Vector2 offset = Random.insideUnitCircle * wanderRadius;
        Vector3 candidate = hubGroundPosition + new Vector3(offset.x, 0f, offset.y);
        return NavMesh.SamplePosition(candidate, out var hit, wanderRadius, NavMesh.AllAreas)
            ? hit.position
            : hubGroundPosition;
    }

    private void OnNight()
    {
        isNight = true;
        foreach (var go in activeCitizens)
        {
            Vector3 destination = PickHomeDestination();
            go.GetComponent<CitizenNPC>().GoHome(destination, () => poolManager.Despawn(go));
        }

        activeCitizens.Clear();
    }

    private Vector3 PickHomeDestination()
    {
        if (homePoints != null && homePoints.Length > 0)
            return homePoints[Random.Range(0, homePoints.Length)].position;
        return hubGroundPosition;
    }

    private void OnDay()
    {
        isNight = false;
        SyncVisibleCountAsync(lifetimeCts.Token).Forget();
    }
}
