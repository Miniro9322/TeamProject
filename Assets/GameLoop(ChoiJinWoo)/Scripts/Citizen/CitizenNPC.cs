using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class CitizenNPC : MonoBehaviour
{
    private const string MovingBool = "IsMoving";
    private const string SocializingBool = "IsSocializing";
    private const float HomeTimeout = 15f;
    private const int MaxDestinationAttempts = 5;

    [SerializeField] private float minWanderInterval = 2f;
    [SerializeField] private float maxWanderInterval = 5f;
    [SerializeField] private float arriveDistance = 0.3f;
    [SerializeField] private float minWanderDistance = 1.5f;
    [SerializeField] private float rotationSpeed = 6f;
    [SerializeField] private float statVariance = 0.2f;
    [SerializeField] private float homeSpeedMultiplier = 1.6f;

    private NavMeshAgent agent;
    private Animator animator;
    private float baseSpeed;

    private Vector3 wanderCenter;
    private float wanderRadius;
    private float nextWanderTime;

    private bool goingHome;
    private float homeDeadline;
    private Action onArrivedHome;

    private bool waitingAtDestination;
    private NavMeshPath scratchPath;

    public bool IsInteracting { get; private set; }
    public bool IsInGroup => isJoiningGroup || isInGroupChat;
    public bool IsAvailableForSocial => !goingHome && !IsInteracting && !IsInGroup;

    private Vector3 greetFacePoint;
    private string greetAnimTrigger;
    private float greetAnimTime;
    private float greetEndTime;
    private bool greetAnimPlayed;

    private bool isJoiningGroup;
    private float joinDeadline;
    private Vector3 groupFacePoint;
    private Action onJoinArrived;

    private bool isInGroupChat;
    private string groupChatAnimTrigger;
    private bool groupChatAnimPlayed;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        scratchPath = new NavMeshPath();

        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

        agent.updateRotation = false;

        agent.speed *= RandomMultiplier();
        rotationSpeed *= RandomMultiplier();
        minWanderInterval *= RandomMultiplier();
        maxWanderInterval *= RandomMultiplier();

        baseSpeed = agent.speed;
    }

    private float RandomMultiplier() => 1f + UnityEngine.Random.Range(-statVariance, statVariance);

    private void SetSocializing(bool value)
    {
        if (animator != null) animator.SetBool(SocializingBool, value);
    }

    private void OnDisable()
    {
        goingHome = false;
        onArrivedHome = null;
        IsInteracting = false;
        isJoiningGroup = false;
        isInGroupChat = false;
        onJoinArrived = null;
        SetSocializing(false);
        if (agent != null)
        {
            agent.speed = baseSpeed;
            if (agent.isOnNavMesh)
                agent.ResetPath();
        }
    }

    public void StartWandering(Vector3 center, float radius)
    {
        wanderCenter = center;
        wanderRadius = radius;
        agent.speed = baseSpeed;
        goingHome = false;
        onArrivedHome = null;
        IsInteracting = false;
        isJoiningGroup = false;
        isInGroupChat = false;
        onJoinArrived = null;
        SetSocializing(false);
        waitingAtDestination = false;
        PickNewWanderDestination();
    }

    public void GoHome(Vector3 homePosition, Action onArrived)
    {
        goingHome = true;
        onArrivedHome = onArrived;
        homeDeadline = Time.time + HomeTimeout;
        IsInteracting = false;
        isJoiningGroup = false;
        isInGroupChat = false;
        onJoinArrived = null;
        SetSocializing(false);
        agent.speed = baseSpeed * homeSpeedMultiplier;
        agent.SetDestination(homePosition);
    }

    public void JoinGroup(Vector3 spotPosition, Vector3 facePoint, float timeout, Action onArrived)
    {
        isJoiningGroup = true;
        groupFacePoint = facePoint;
        joinDeadline = Time.time + timeout;
        onJoinArrived = onArrived;
        agent.SetDestination(spotPosition);
    }

    public void StartGroupChat(string animTrigger)
    {
        isJoiningGroup = false;
        isInGroupChat = true;
        groupChatAnimTrigger = animTrigger;
        groupChatAnimPlayed = false;
        SetSocializing(true);
        agent.SetDestination(transform.position);
    }

    public void EndGroupChat()
    {
        isInGroupChat = false;
        SetSocializing(false);
        waitingAtDestination = false;
    }

    public void TriggerGreet(Vector3 facePoint, float animDelay, float duration, string animTrigger)
    {
        IsInteracting = true;
        SetSocializing(true);
        agent.SetDestination(transform.position);

        greetFacePoint = facePoint;
        greetAnimTrigger = animTrigger;
        greetAnimTime = Time.time + animDelay;
        greetEndTime = Time.time + animDelay + duration;
        greetAnimPlayed = false;
    }

    private void Update()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        if (IsInteracting)
        {
            UpdateGreet();
            return;
        }

        if (isInGroupChat)
        {
            UpdateGroupChat();
            return;
        }

        if (isJoiningGroup)
        {
            UpdateJoinGroup();
            return;
        }

        bool pathReady = !agent.pathPending;
        bool arrived = pathReady && agent.remainingDistance <= arriveDistance;

        bool moving = pathReady && !arrived && agent.velocity.sqrMagnitude > 0.01f;
        if (animator != null) animator.SetBool(MovingBool, moving);
        if (moving) FaceMovementDirection();

        if (goingHome)
        {
            if (arrived || Time.time >= homeDeadline)
            {
                goingHome = false;
                var callback = onArrivedHome;
                onArrivedHome = null;
                callback?.Invoke();
            }
            return;
        }

        if (!pathReady) return;

        if (!arrived)
        {
            waitingAtDestination = false;
            return;
        }

        if (!waitingAtDestination)
        {
            waitingAtDestination = true;
            nextWanderTime = Time.time + UnityEngine.Random.Range(minWanderInterval, maxWanderInterval);
            return;
        }

        if (Time.time >= nextWanderTime)
            PickNewWanderDestination();
    }

    private void UpdateGreet()
    {
        if (animator != null) animator.SetBool(MovingBool, false);
        FaceTowardDirection(greetFacePoint - transform.position);

        if (!greetAnimPlayed && Time.time >= greetAnimTime)
        {
            greetAnimPlayed = true;
            if (animator != null && !string.IsNullOrEmpty(greetAnimTrigger))
                animator.SetTrigger(greetAnimTrigger);
        }

        if (Time.time >= greetEndTime)
        {
            IsInteracting = false;
            SetSocializing(false);
            waitingAtDestination = false;
        }
    }

    private void UpdateJoinGroup()
    {
        bool pathReady = !agent.pathPending;
        bool arrived = pathReady && agent.remainingDistance <= arriveDistance;

        bool moving = pathReady && !arrived && agent.velocity.sqrMagnitude > 0.01f;
        if (animator != null) animator.SetBool(MovingBool, moving);
        if (moving) FaceMovementDirection();
        else if (arrived) FaceTowardDirection(groupFacePoint - transform.position);

        if (arrived || Time.time >= joinDeadline)
        {
            isJoiningGroup = false;
            var callback = onJoinArrived;
            onJoinArrived = null;
            callback?.Invoke();
        }
    }

    private void UpdateGroupChat()
    {
        if (animator != null) animator.SetBool(MovingBool, false);
        FaceTowardDirection(groupFacePoint - transform.position);

        if (!groupChatAnimPlayed)
        {
            groupChatAnimPlayed = true;
            if (animator != null && !string.IsNullOrEmpty(groupChatAnimTrigger))
                animator.SetTrigger(groupChatAnimTrigger);
        }
    }

    private void FaceMovementDirection() => FaceTowardDirection(agent.velocity);

    private void FaceTowardDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
    }

    private void PickNewWanderDestination()
    {
        waitingAtDestination = false;
        agent.SetDestination(FindWanderDestination());
    }

    private Vector3 FindWanderDestination()
    {
        float minSqr = minWanderDistance * minWanderDistance;
        Vector3 fallback = wanderCenter;

        for (int attempt = 0; attempt < MaxDestinationAttempts; attempt++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * wanderRadius;
            Vector3 candidate = wanderCenter + new Vector3(offset.x, 0f, offset.y);

            if (!NavMesh.SamplePosition(candidate, out var hit, wanderRadius, NavMesh.AllAreas))
                continue;

            if (!NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, scratchPath)
                || scratchPath.status != NavMeshPathStatus.PathComplete)
                continue;

            fallback = hit.position;
            if ((hit.position - transform.position).sqrMagnitude >= minSqr)
                return hit.position;
        }

        return fallback;
    }
}
