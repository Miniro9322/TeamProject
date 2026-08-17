#ifndef FOG_CORE_INCLUDED
#define FOG_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

float4 _FogAreas[8];
float _FogOpens[8];
int _FogCount;
float4 _FogSafeAreas[8];
int _FogSafeCount;
float _FogCoverMargin;
float _FogSafeMargin;

// FogLook가 인스펙터 값으로 밀어주는 외형 파라미터
float4 _FogColor;
float _FogDensity;
float _CloudScale;  // 색 구름 주기
float _CloudTint;   // 색 구름 대비
float _CloudWindSpeed;

void FogWorld_float(
    float2 UV,
    out float3 World,
    out float Valid)
{
#if UNITY_REVERSED_Z
    float depth = SampleSceneDepth(UV);
    Valid = depth > 0.0001 ? 1.0 : 0.0;
#else
    float raw = SampleSceneDepth(UV);
    float depth = lerp(
        UNITY_NEAR_CLIP_VALUE,
        1.0,
        raw
    );

    Valid = raw < 0.9999 ? 1.0 : 0.0;
#endif

    World = ComputeWorldSpacePosition(
        UV,
        depth,
        UNITY_MATRIX_I_VP
    );
}

float BoxDist(float2 pos, float4 area)
{
    float2 delta = abs(pos - area.xy) - area.zw;
    float outside = length(max(delta, 0.0));
    float inside = min(max(delta.x, delta.y), 0.0);

    return outside + inside;
}

void FogMask_float(
    float3 World,
    float Noise,
    float Edge,
    float Rough,
    out float Mask)
{
    Mask = 1.0;

    [unroll]
    for (int index = 0; index < 8; index++)
    {
        if (index >= _FogCount)
        {
            break;
        }

        float dist = BoxDist(
            World.xz,
            _FogAreas[index]
        );

        dist += (Noise - 0.5) * Rough;

        float hole = smoothstep(
            -Edge,
            Edge,
            dist
        );

        hole = lerp(
            1.0,
            hole,
            saturate(_FogOpens[index])
        );

        Mask = min(Mask, hole);
    }
}

// ---------------------------------------------------------------------------
// 그래프 단순화를 위한 통합 함수 + 구름 노이즈.
// 그래프는 ScreenPosition -> FogFinal -> (BaseColor, Alpha) 만 이으면 된다.
// ---------------------------------------------------------------------------

float FogHash(float2 p)
{
    p = frac(p * float2(123.34, 345.45));
    p += dot(p, p + 34.345);
    return frac(p.x * p.y);
}

float FogValueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    float a = FogHash(i + float2(0.0, 0.0));
    float b = FogHash(i + float2(1.0, 0.0));
    float c = FogHash(i + float2(0.0, 1.0));
    float d = FogHash(i + float2(1.0, 1.0));
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

float FogNoise(float2 p)
{
    float n = 0.0;
    n += 0.5   * FogValueNoise(p);
    n += 0.25  * FogValueNoise(p * 2.03);
    n += 0.125 * FogValueNoise(p * 4.01);
    return n / 0.875;
}

float FogSampleDepth01(float2 uv)
{
#if UNITY_REVERSED_Z
    return SampleSceneDepth(uv);
#else
    return lerp(UNITY_NEAR_CLIP_VALUE, 1.0, SampleSceneDepth(uv));
#endif
}

float FogLockedCoverage(float areaDistance)
{
    return step(areaDistance, _FogCoverMargin);
}

float FogOpenedCoverage(float areaDistance)
{
    return step(areaDistance, 0.0);
}

float FogSafeCoverage(float areaDistance)
{
    return step(areaDistance, _FogSafeMargin);
}

float FogModuleMask(float2 worldPosition)
{
    float moduleMask = 0.0;
    float openedMask = 0.0;
    int areaCount = _FogCount;

    [unroll]
    for (int areaIndex = 0; areaIndex < 8; areaIndex++)
    {
        if (areaIndex >= areaCount) { break; }

        float areaDistance = BoxDist(worldPosition, _FogAreas[areaIndex]);
        float lockedCoverage = FogLockedCoverage(areaDistance);
        float openedCoverage = FogOpenedCoverage(areaDistance);
        float openAmount = saturate(_FogOpens[areaIndex]);

        moduleMask = max(moduleMask, lockedCoverage * (1.0 - openAmount));
        openedMask = max(openedMask, openedCoverage * openAmount);
    }

    return min(moduleMask, 1.0 - openedMask);
}

float FogSafeMask(float2 worldPosition)
{
    float safeMask = 0.0;
    int safeAreaCount = _FogSafeCount;

    [unroll]
    for (int safeAreaIndex = 0; safeAreaIndex < 8; safeAreaIndex++)
    {
        if (safeAreaIndex >= safeAreaCount) { break; }

        float safeDistance = BoxDist(worldPosition, _FogSafeAreas[safeAreaIndex]);
        safeMask = max(safeMask, FogSafeCoverage(safeDistance));
    }

    return safeMask;
}

// Calculates only the final covered area from a world position.
float CalculateFogAreaMask(float2 worldPosition)
{
    float moduleMask = FogModuleMask(worldPosition);
    float safeMask = FogSafeMask(worldPosition);

    return moduleMask * (1.0 - safeMask);
}

// Outputs only the covered area for Shader Graph.
void FogAreaMask_float(
    float2 UV,
    out float AreaMask)
{
    float depth = FogSampleDepth01(UV);
    float3 worldPosition = ComputeWorldSpacePosition(
        UV,
        depth,
        UNITY_MATRIX_I_VP
    );

    AreaMask = CalculateFogAreaMask(worldPosition.xz);
}

void FogFinal_float(
    float2 UV,
    out float3 Color,
    out float Alpha)
{
    // 1) 이 픽셀의 실제 지면 월드좌표
    float raw = SampleSceneDepth(UV);
#if UNITY_REVERSED_Z
    float depth = raw;
#else
    float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, raw);
#endif
    float3 world = ComputeWorldSpacePosition(UV, depth, UNITY_MATRIX_I_VP);

    // 2) Samples two cloud layers with independent visual wind movement.
    float2 cloudWind = _Time.y * _CloudWindSpeed;
    float2 uvA = (world.xz + cloudWind) * _CloudScale;
    float2 uvB = (world.xz + cloudWind * 1.7 + 37.0) * (_CloudScale * 0.5);
    float cloud = FogNoise(uvA) * 0.6 + FogNoise(uvB) * 0.4;

    // 3) Reads the shared area result without changing its calculation.
    float mask = CalculateFogAreaMask(world.xz);

    // 4) 출력 — 잠긴 모듈 자리에서만 불투명. 하늘은 어떤 박스에도 안 들어가 mask=0.
    Color = _FogColor.rgb * (1.0 - _CloudTint + _CloudTint * cloud);
    Alpha = mask * _FogDensity;
}

#endif
