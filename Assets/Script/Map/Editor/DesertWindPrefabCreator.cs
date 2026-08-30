using UnityEditor;
using UnityEngine;

// 사막 지대 전체를 덮는 낮 모래 흐름용 파티클 프리팹과 재질을 만드는 에디터 도구.
public static class DesertWindPrefabCreator
{
    private const string FolderPath = "Assets/Resources/ZoneEffectPrefab";
    private const string PrefabPath = FolderPath + "/DesertWindFlowVFX.prefab";
    private const string MaterialPath = FolderPath + "/DesertWindFlow.mat";
    private const string GrainTextureName = "Default-Particle.psd";
    private const string ParticleShaderName = "Universal Render Pipeline/Particles/Unlit";
    private const string ObjectName = "DesertWindFlow";

    private const float LoopDuration = 4f;
    private const float ShortLifetime = 1.2f;
    private const float LongLifetime = 2f;
    private const float SmallGrainSize = 0.18f;
    private const float LargeGrainSize = 0.4f;
    private const float EmitRatePerSecond = 900f;
    private const int MaxGrainCount = 1600;
    private const float SpawnBoxHeight = 0.6f;
    private const float SpawnBoxSide = 1f;
    private const float FadeInRatio = 0.15f;
    private const float FadeOutRatio = 0.8f;
    private const float TrailKeepRatio = 1f;
    private const float TrailLifetime = 0.28f;
    private const float TrailWidth = 0.3f;
    private const float TransparentSurface = 1f;
    private const float AlphaBlendMode = 0f;

    private static readonly Color SandGrainColor = new Color(1.3f, 1.1f, 0.62f, 1f);

    // 메뉴에서 눌러 재질과 프리팹을 한 번에 만든다.
    [MenuItem("Tools/Effects/Create DesertWindFlow Prefab")]
    public static void CreatePrefab()
    {
        GameObject flowObject = new GameObject(ObjectName);
        ParticleSystem particles = flowObject.AddComponent<ParticleSystem>();

        SetupMainModule(particles);
        SetupEmission(particles);
        SetupSpawnBox(particles);
        SetupFlowVelocity(particles);
        SetupFadeColor(particles);
        SetupTrails(particles);
        SetupRenderer(flowObject.GetComponent<ParticleSystemRenderer>());

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(flowObject, PrefabPath);
        Object.DestroyImmediate(flowObject);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = saved;
        EditorGUIUtility.PingObject(saved);
        Debug.Log("[DesertWindFlow] 프리팹 생성 완료: " + PrefabPath);
    }

    // 알갱이 크기와 수명, 좌표 기준 같은 기본 값을 정한다.
    private static void SetupMainModule(ParticleSystem particles)
    {
        ParticleSystem.MainModule main = particles.main;
        main.duration = LoopDuration;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(ShortLifetime, LongLifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0f);
        main.startSize = new ParticleSystem.MinMaxCurve(SmallGrainSize, LargeGrainSize);
        main.startColor = SandGrainColor;
        main.gravityModifier = new ParticleSystem.MinMaxCurve(0f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = MaxGrainCount;
    }

    // 초당 몇 알갱이를 뿌릴지 정한다.
    private static void SetupEmission(ParticleSystem particles)
    {
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(EmitRatePerSecond);
    }

    // 보드 전체를 덮을 상자 모양으로 뿌린다. 실제 가로세로는 게임에서 보드 크기로 늘린다.
    private static void SetupSpawnBox(ParticleSystem particles)
    {
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(SpawnBoxSide, SpawnBoxHeight, SpawnBoxSide);
    }

    // 바람 방향을 넣을 통로를 열어 둔다. 실제 방향 값은 게임에서 매일 넣는다.
    private static void SetupFlowVelocity(ParticleSystem particles)
    {
        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(0f);
        velocity.y = new ParticleSystem.MinMaxCurve(0f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f);
    }

    // 알갱이가 나타났다 사라지게 해 뚝 끊기는 느낌을 없앤다.
    private static void SetupFadeColor(ParticleSystem particles)
    {
        ParticleSystem.ColorOverLifetimeModule fade = particles.colorOverLifetime;
        fade.enabled = true;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f),
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, FadeInRatio),
                new GradientAlphaKey(1f, FadeOutRatio),
                new GradientAlphaKey(0f, 1f),
            });
        fade.color = new ParticleSystem.MinMaxGradient(gradient);
    }

    // 알갱이마다 짧은 꼬리를 남겨 흐르는 방향이 보이게 한다.
    private static void SetupTrails(ParticleSystem particles)
    {
        ParticleSystem.TrailModule trails = particles.trails;
        trails.enabled = true;
        trails.mode = ParticleSystemTrailMode.PerParticle;
        trails.ratio = TrailKeepRatio;
        trails.lifetime = new ParticleSystem.MinMaxCurve(TrailLifetime);
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(TrailWidth);
        trails.inheritParticleColor = true;
    }

    // 알갱이와 꼬리를 같은 모래 재질로 그린다.
    private static void SetupRenderer(ParticleSystemRenderer renderer)
    {
        Material sandMaterial = CreateMaterial();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        renderer.sharedMaterial = sandMaterial;
        renderer.trailMaterial = sandMaterial;
    }

    // 흙먼지 그림을 입힌 반투명 모래 재질을 새로 만들어 저장한다.
    private static Material CreateMaterial()
    {
        Shader particleShader = Shader.Find(ParticleShaderName);
        Texture2D grainTexture = AssetDatabase.GetBuiltinExtraResource<Texture2D>(GrainTextureName);
        RequireShader(particleShader);
        RequireTexture(grainTexture);

        AssetDatabase.DeleteAsset(MaterialPath);

        Material material = new Material(particleShader);
        material.name = ObjectName;
        material.SetFloat("_Surface", TransparentSurface);
        material.SetFloat("_Blend", AlphaBlendMode);
        material.SetTexture("_BaseMap", grainTexture);
        AssetDatabase.CreateAsset(material, MaterialPath);
        return material;
    }

    // 파티클 셰이더가 없으면 즉시 실패시킨다.
    private static void RequireShader(Shader particleShader)
    {
        if (particleShader == null)
        {
            throw new System.InvalidOperationException("[DesertWindFlow] 셰이더를 찾지 못했다: " + ParticleShaderName);
        }
    }

    // 알갱이 그림이 없으면 즉시 실패시킨다.
    private static void RequireTexture(Texture2D grainTexture)
    {
        if (grainTexture == null)
        {
            throw new System.InvalidOperationException("[DesertWindFlow] 그림을 찾지 못했다: " + GrainTextureName);
        }
    }
}
