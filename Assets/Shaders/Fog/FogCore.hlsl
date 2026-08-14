#ifndef FOG_CORE_INCLUDED
#define FOG_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

float4 _FogAreas[8];
float _FogOpens[8];
int _FogCount;

// FogLook가 인스펙터 값으로 밀어주는 외형 파라미터
float4 _FogColor;
float _FogDensity;
float _EdgeSoft;    // 경계 부드러움
float _EdgeRough;   // 경계 흔들림 진폭(직선 깨는 핵심)
float _EdgeScale;   // 경계 노이즈 주기(로브 크기)
float _EdgeMargin;  // 모듈 외곽에서 바깥으로 더 걷어낼 거리(월드)
float _CloudScale;  // 색 구름 주기
float _CloudTint;   // 색 구름 대비
float _WindSpeed;

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

    // 2) 색 구름 — 서로 다른 속도·주기로 흐르는 2겹으로 뭉게뭉게. _WindSpeed는 월드/초.
    float2 wind = _Time.y * _WindSpeed;
    float2 uvA = (world.xz + wind) * _CloudScale;
    float2 uvB = (world.xz + wind * 1.7 + 37.0) * (_CloudScale * 0.5);
    float cloud = FogNoise(uvA) * 0.6 + FogNoise(uvB) * 0.4;

    // 3) 경계 전용 노이즈 — 느리게 흐르는 큰 덩어리로 외곽선을 허문다.
    float2 edgeUv = (world.xz + wind * 0.4) * _EdgeScale;
    float edgeNoise = FogNoise(edgeUv);

    // 4) 잠금 마스크 (1=안개, 0=맨눈). 기본은 안개 없음 — 잠긴 모듈 발자국 안에서만 낀다.
    //    판정이 XZ 좌표라 타일 높이와 무관하다: 외곽 벽의 측면도 같이 덮인다.
    //    경계는 노이즈로 크게 허문다 — 안개는 격자를 모른다. 발자국 사각형을 따라 각지면 안 된다.
    float soft = max(_EdgeSoft, 1e-3);
    float mask = 0.0;
    float opened = 0.0;
    int count = _FogCount;
    [unroll]
    for (int i = 0; i < 8; i++)
    {
        if (i >= count) { break; }
        float raw = BoxDist(world.xz, _FogAreas[i]);
        float dist = raw - _EdgeMargin + (edgeNoise - 0.5) * _EdgeRough;
        float inside = 1.0 - smoothstep(-soft, 0.0, dist);
        float open = saturate(_FogOpens[i]);

        mask = max(mask, inside * (1.0 - open));   // 개방될수록 걷힌다
        // 열린 모듈의 발자국 안(노이즈 없는 원래 사각형)은 이웃 안개가 넘어오지 못하게 지킨다.
        // 사각형 안(raw<=0)은 전부 보호하고, 밖으로만 soft만큼 풀린다. 경계에서 반만 지키면 테두리가 뿌옇게 남는다.
        // 보호 경계도 노이즈로 허문다. 빼기만 하므로 구멍은 밖으로만 커지고 발자국 안은 절대 안 먹힌다.
        float wob = raw - edgeNoise * _EdgeRough;
        opened = max(opened, open * (1.0 - smoothstep(-soft, 0.0, wob)));
    }
    mask *= 1.0 - opened;

    // 5) 출력 — 잠긴 모듈 자리에서만 불투명. 하늘은 어떤 박스에도 안 들어가 mask=0.
    Color = _FogColor.rgb * (1.0 - _CloudTint + _CloudTint * cloud);
    Alpha = mask * _FogDensity;
}

#endif
