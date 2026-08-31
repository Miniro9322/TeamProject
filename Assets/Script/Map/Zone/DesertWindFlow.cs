using UnityEngine;

// 사막 보드 전체를 덮는 낮 모래 흐름을 만들고 켜고 끄는 표시 전담.
public class DesertWindFlow
{
    private const float GrainFlowSpeed = 3.2f;
    private const float GrainHeightAboveBoard = 0.35f;

    private readonly MapBoard board;
    private readonly ParticleSystem particles;
    private readonly Vector3 boardSize;

    // 보드 크기에 맞춘 흐름 파티클을 보드 자식으로 하나 만들어 꺼 둔 채로 보관한다.
    public DesertWindFlow(MapBoard board, GameObject flowPrefab)
    {
        this.board = board;

        Bounds bounds = board.WorldBounds;
        boardSize = bounds.size;
        Vector3 spawnCenter = new Vector3(bounds.center.x, bounds.max.y + GrainHeightAboveBoard, bounds.center.z);
        GameObject instance = Object.Instantiate(flowPrefab, spawnCenter, Quaternion.identity, board.transform);
        particles = instance.GetComponent<ParticleSystem>();

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.scale = new Vector3(bounds.size.x, shape.scale.y, bounds.size.z);
        instance.SetActive(false);
    }

    // 그날 바람 방향으로 뿌리는 자리와 흐름을 맞춘 뒤 켠다.
    public void ShowFlow(Vector2Int windDirection)
    {
        Vector3 flowDirection = WorldDirectionCalc.ReadDirection(board, windDirection);
        SetSpawnArea(flowDirection);
        SetFlowDirection(flowDirection * GrainFlowSpeed);
        PlayFlow();
    }

    // 흐름을 멈추고 남은 알갱이까지 지운다.
    public void HideFlow()
    {
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particles.gameObject.SetActive(false);
    }

    // 알갱이가 흘러갈 거리만큼 뿌리는 상자를 줄이고 바람 반대쪽으로 밀어 보드 밖으로 못 나가게 한다.
    private void SetSpawnArea(Vector3 flowDirection)
    {
        float travelDistance = GrainFlowSpeed * particles.main.startLifetime.constantMax;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.scale = new Vector3(
            boardSize.x - Mathf.Abs(flowDirection.x) * travelDistance,
            shape.scale.y,
            boardSize.z - Mathf.Abs(flowDirection.z) * travelDistance);
        shape.position = new Vector3(
            -flowDirection.x * travelDistance * 0.5f,
            0f,
            -flowDirection.z * travelDistance * 0.5f);
    }

    // 완성된 월드 속도를 파티클의 가로세로 이동 값에 그대로 넣는다.
    private void SetFlowDirection(Vector3 worldVelocity)
    {
        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.x = new ParticleSystem.MinMaxCurve(worldVelocity.x);
        velocity.z = new ParticleSystem.MinMaxCurve(worldVelocity.z);
    }

    // 꺼져 있던 흐름 오브젝트를 켜고 재생을 시작한다.
    private void PlayFlow()
    {
        particles.gameObject.SetActive(true);
        particles.Play();
    }
}
