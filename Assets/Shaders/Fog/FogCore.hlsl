#ifndef FOG_CORE_INCLUDED
#define FOG_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

#define FOG_MAX_AREAS 12

float4 _FogAreas[FOG_MAX_AREAS];
float _FogOpens[FOG_MAX_AREAS];
int _FogCount;

// FogLook가 인스펙터 값으로 밀어주는 외형 파라미터
float4 _FogColor;      // 구름 그림자(두께가 얇은 곳) 색
float4 _CloudHighlight; // 구름 덩어리 상단(두꺼운 곳) 밝은 색
float _FogDensity;
float _EdgeSoft;    // 경계 부드러움
float _EdgeRough;   // 경계 흔들림 진폭(직선 깨는 핵심)
float _EdgeScale;   // 경계 노이즈 주기(로브 크기)
float _EdgeMargin;  // 모듈 외곽에서 바깥으로 더 걷어낼 거리(월드)
float _CloudScale;  // 구름 덩어리 주기(작을수록 덩어리가 커짐)
float _CloudTint;   // 그림자<->하이라이트 대비 강도
float _CloudCoverage; // 구름 덩어리 문턱값(낮을수록 구름이 넓게 뒤덮음)
float _CloudSoftness;  // 구름 덩어리 경계 부드러움
float _CloudContrast;   // 문턱값 기준으로 밝은/어두운 영역을 더 벌려서 또렷하게 만드는 대비
float _CloudMinAlpha;  // 구름 틈에서도 유지되는 최소 불투명도
float _CloudShadeStrength;  // 방향성 명암(하이라이트/그림자) 강도 — 뭉텅이의 입체감
float _CloudShadeSharpness; // 경사가 급한 곳(뭉텅이 옆면)에만 명암이 몰리는 정도

// 안개가 빛을 가려 바깥 맨눈 지형에 드리우는 가짜 그림자 — "위에 떠 있다"는 착시의 핵심.
float _ShadowOffsetDist; // 안개 경계에서 그림자가 뻗어나가는 거리(월드)
float _ShadowSoft;       // 그림자 자체 경계의 부드러움
float _ShadowDarken;     // 그림자 부분의 어두워지는 정도(0~1, 낮을수록 더 어두움)
float _ShadowStrength;   // 그림자 전체 강도(불투명도)
float _CloudAltitude;  // 구름 무늬를 투영할 가상 수평면의 월드 높이(패럴랙스용)
float _WindSpeed;

// 시야 경계선 — 카메라 각도와 무관하게 "여긴 다른 상태다"를 알려주는 또렷한 테두리.
float4 _EdgeLineColor;
float _EdgeLineSharpness; // 높을수록 선이 얇아짐
float _EdgeLineStrength;  // 0이면 선 없음, 1이면 완전히 테두리색

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
    for (int index = 0; index < FOG_MAX_AREAS; index++)
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

// 문명(civ) 스타일 뭉게구름 덩어리 모양. 빌로우(turbulence) 노이즈 + 도메인 워프로
// 매끈한 안개 대신 퍼프가 뭉쳐진 덩어리 실루엣을 만든다.
float FogBillow(float2 p)
{
    return abs(FogValueNoise(p) * 2.0 - 1.0);
}

float FogCloudShape(float2 p)
{
    // 워프가 강하면(옛 1.4) 노이즈가 한쪽으로 크게 휘저어져 대리석/유체 흐름처럼 줄무늬가
    // 이어진다. 문명 스타일 뭉텅이는 워프를 약하게만 써서 국소적으로만 뒤틀리게 해야 한다.
    float2 warp = float2(FogValueNoise(p * 0.5 + 11.0), FogValueNoise(p * 0.5 + 53.0)) - 0.5;
    float2 wp = p + warp * 0.3;

    // 실루엣(저주파 2단)은 일반 노이즈로 뭉텅뭉텅한 둥근 덩어리를 만들고,
    // 표면 질감(고주파 2단)만 빌로우로 얹는다. 빌로우를 전 옥타브에 쓰면
    // 노이즈가 0.5를 지나는 곳마다 골이 생겨 대리석/실핏줄 무늬가 되어
    // 둥근 뭉텅이 대신 가는 줄무늬로 보인다 — 그래서 실루엣은 빌로우를 피한다.
    float n = 0.0;
    n += 0.55 * FogValueNoise(wp);
    n += 0.25 * FogValueNoise(wp * 2.11);
    n += 0.12 * FogBillow(wp * 4.42);
    n += 0.08 * FogBillow(wp * 9.03);
    return n;
}

// 문턱값(_CloudCoverage)을 기준점으로 값을 밀어 붙여, 중간 회색이 뭉게뭉게 뭉치지 않고
// 또렷하게 밝은/어두운 영역으로 갈라지게 한다. 대비가 없으면 옥타브를 더해도 전체가 뿌옇게 보인다.
float FogCloudContrast(float shape)
{
    return saturate((shape - _CloudCoverage) * _CloudContrast + _CloudCoverage);
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
    float validDepth = raw > 0.0001 ? 1.0 : 0.0;
#else
    float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, raw);
    float validDepth = raw < 0.9999 ? 1.0 : 0.0;
#endif
    float3 world = ComputeWorldSpacePosition(UV, depth, UNITY_MATRIX_I_VP);

    // 1.5) 구름 무늬는 실제 표면(지형/벽/지붕) 위치가 아니라, 그 픽셀의 시선이
    //      고도 _CloudAltitude인 가상 수평면과 만나는 지점을 기준으로 샘플링한다.
    //      그래야 벽·건물 표면에 무늬가 눌어붙지 않고, 카메라가 움직일 때 지형과는
    //      다른 시차(패럴랙스)로 움직여 "위에 떠 있는 레이어"처럼 보인다.
    //      (world는 이미 카메라→실제 표면 방향의 광선 위 한 점이므로 방향만 재사용한다.)
    float3 camPos = _WorldSpaceCameraPos.xyz;
    float3 rayDir = world - camPos;
    float tPlane = (abs(rayDir.y) > 1e-5) ? (_CloudAltitude - camPos.y) / rayDir.y : 1.0;
    float3 cloudWorld = camPos + rayDir * tPlane;

    // 2) 뭉게구름 덩어리 — 빌로우 노이즈로 퍼프 실루엣을 만들고 문턱값으로 덩어리를 도려낸다.
    //    _WindSpeed는 월드/초 흐름 속도.
    float2 wind = _Time.y * _WindSpeed;
    float2 uvA = (cloudWorld.xz + wind) * _CloudScale;
    float cloudShape = FogCloudContrast(FogCloudShape(uvA));
    float coverage = smoothstep(_CloudCoverage - _CloudSoftness, _CloudCoverage + _CloudSoftness, cloudShape);

    // 2.5) 뭉텅이를 높이맵처럼 보고 그 경사(그래디언트)로 방향성 명암을 낸다.
    //      평평한 꼭대기·깊은 틈에서는 경사가 거의 없어 명암이 거의 안 생기고,
    //      뭉텅이의 둥근 옆면(경사가 급한 곳)에서만 빛/그림자가 몰려 진짜 퍼프처럼 보인다.
    const float2 gradEps = float2(0.05, 0.05);
    float shapeX = FogCloudContrast(FogCloudShape(uvA + float2(gradEps.x, 0.0)));
    float shapeY = FogCloudContrast(FogCloudShape(uvA + float2(0.0, gradEps.y)));
    float2 grad = float2(shapeX - cloudShape, shapeY - cloudShape) / gradEps;

    const float2 cloudLightDir = float2(0.6963, 0.7177); // 좌상단에서 비추는 가상 광원 방향(정규화됨)
    float gradLen = length(grad);
    float2 gradDir = gradLen > 1e-5 ? grad / gradLen : float2(0.0, 0.0);
    float slope = dot(gradDir, cloudLightDir) * saturate(gradLen * _CloudShadeSharpness);

    // 3) 경계 전용 노이즈 — 느리게 흐르는 큰 덩어리로 외곽선을 허문다.
    float2 edgeUv = (world.xz + wind * 0.4) * _EdgeScale;
    float edgeNoise = FogNoise(edgeUv);

    // 4) 잠금 마스크 (1=안개, 0=맨눈). 기본은 안개 없음 — 잠긴 모듈 발자국 안에서만 낀다.
    //    판정이 XZ 좌표라 타일 높이와 무관하다: 외곽 벽의 측면도 같이 덮인다.
    //    경계는 노이즈로 크게 허문다 — 안개는 격자를 모른다. 발자국 사각형을 따라 각지면 안 된다.
    float soft = max(_EdgeSoft, 1e-3);
    float mask = 0.0;
    float opened = 0.0;
    float shadowAmount = 0.0;
    float2 shadowOffset = -cloudLightDir * _ShadowOffsetDist; // 빛이 오는 반대쪽으로 그림자가 뻗는다.
    int count = _FogCount;
    [unroll]
    for (int i = 0; i < FOG_MAX_AREAS; i++)
    {
        if (i >= count) { break; }
        float raw = BoxDist(world.xz, _FogAreas[i]);
        float dist = raw - _EdgeMargin + (edgeNoise - 0.5) * _EdgeRough;
        float inside = 1.0 - smoothstep(-soft, soft, dist);
        float open = saturate(_FogOpens[i]);

        mask = max(mask, inside * (1.0 - open));   // 개방될수록 걷힌다
        // 열린 모듈의 발자국은 안개가 넘어오지 못하게 지키되, 노이즈는 바깥쪽으로만 깎아내게 한다
        // (max(...,0)로 안쪽을 파고드는 방향은 막음) — 그래야 각진 사각형이 아니라 옆 안개처럼
        // 자연스럽게 울렁이는 경계로 이어지면서도, 이미 열린 자리가 다시 덮이는 일은 없다.
        float openDist = raw - max(edgeNoise - 0.5, 0.0) * _EdgeRough;
        opened = max(opened, open * (1.0 - smoothstep(-soft, soft, openDist)));

        // 아직 잠긴 구역만 그림자를 드리운다 — 같은 사각형을 빛 반대 방향으로 밀어서 재판정.
        float shadowDist = BoxDist(world.xz + shadowOffset, _FogAreas[i]);
        float shadowInside = 1.0 - smoothstep(-_ShadowSoft, _ShadowSoft, shadowDist);
        shadowAmount = max(shadowAmount, shadowInside * (1.0 - open));
    }
    mask *= 1.0 - opened;
    shadowAmount *= 1.0 - mask; // 안개 자신 위에는 그리지 않고, 안개 밖 맨눈 지형에만 얹는다.

    // 5) 출력 — 잠긴 모듈 자리에서만 불투명. 하늘은 어떤 박스에도 안 들어가 mask=0.
    //    두꺼운 덩어리(coverage 높음)는 밝은 하이라이트, 옅은 틈은 그림자색으로.
    //    거기에 경사 기반 방향성 명암(slope)을 더해 뭉게구름다운 입체감을 낸다.
    float lit = saturate(coverage + slope * _CloudShadeStrength);
    float3 cloudColor = lerp(_FogColor.rgb, _CloudHighlight.rgb, lit * saturate(_CloudTint * 2.0));

    //    덩어리 틈에서도 최소 밀도는 유지해 아래 지형이 다 드러나지 않게 한다.
    float cloudAlphaMul = lerp(_CloudMinAlpha, 1.0, coverage);
    float cloudAlphaOut = mask * _FogDensity * cloudAlphaMul;

    //    맨눈 지형 쪽 — 안개가 빛을 가려 드리운 그림자. 실제 뜬 물체가 없어도 "위에 뭔가 있다"는 단서가 된다.
    float3 shadowColor = _FogColor.rgb * _ShadowDarken;
    float shadowAlphaOut = shadowAmount * _ShadowStrength;

    Color = lerp(shadowColor, cloudColor, mask);
    Alpha = max(cloudAlphaOut, shadowAlphaOut);

    // 6) 시야 경계선 — mask가 딱 절반(0.5)인 지점, 즉 열림/닫힘의 실제 경계에서만 값이 솟는다.
    //    카메라 각도·패럴랙스에 의존하지 않고 "지금 여기가 경계다"를 명확히 보여준다.
    float edgeLine = saturate(1.0 - abs(mask * 2.0 - 1.0));
    edgeLine = pow(edgeLine, max(_EdgeLineSharpness, 1e-3)) * saturate(_EdgeLineStrength);

    Color = lerp(Color, _EdgeLineColor.rgb, edgeLine);
    Alpha = max(Alpha, edgeLine * _EdgeLineColor.a);

    // 7) 단단한 지형이 없는 픽셀(물처럼 깊이가 안 찍히는 곳, 하늘 등)에는 안개를 아예 안 그린다.
    //    깊이가 배경값이면 월드좌표 복원이 엉뚱한 곳을 가리켜 잠금 박스에 우연히 걸릴 수 있는데,
    //    그러면 물 위에 각진 안개 자국이 생긴다 — 이 체크로 그 자국 자체를 차단한다.
    Alpha *= validDepth;
}

#endif
