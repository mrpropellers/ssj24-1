#ifndef MYHLSLINCLUDE_INCLUDED
#define MYHLSLINCLUDE_INCLUDED

TEXTURE2D(_CameraColorTexture);
SAMPLER(sampler_CameraColorTexture);
float4 _CameraColorTexture_TexelSize;

float RemapDepth(float normalizedDepth, float nearClip, float farClip, float newNearClip, float newFarClip)
{
    const float inverseDepth = 1 - normalizedDepth;
    const float eyeDepth = (farClip - nearClip) * inverseDepth;
    return 1 - (eyeDepth - newNearClip) / (newFarClip - newNearClip);
}

float GetDepth(float2 UV, float targetNearClip, float targetFarClip)
{
    float near = _ProjectionParams.y;
    float far = _ProjectionParams.z;
    float depth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, UV).r;
    float normalizedDepth = RemapDepth(depth, near, far, targetNearClip, targetFarClip);
#if !UNITY_REVERSED_Z
    normalizedDepth = 1 - normalizedDepth;
#endif
    return normalizedDepth;
}

float3 GetNormal(float2 UV)
{
    return SAMPLE_TEXTURE2D(_CameraNormalsTexture, sampler_CameraNormalsTexture, UV);
}

float DepthEdgeIndicator(float2 uvSamples[4], float depth, float targetNearClip, float targetFarClip)
{
    float diff = 0.0;
    diff += clamp(GetDepth(uvSamples[0], targetNearClip, targetFarClip) - depth, 0.0, 1.0);
    diff += clamp(GetDepth(uvSamples[1], targetNearClip, targetFarClip) - depth, 0.0, 1.0);
    diff += clamp(GetDepth(uvSamples[2], targetNearClip, targetFarClip) - depth, 0.0, 1.0);
    diff += clamp(GetDepth(uvSamples[3], targetNearClip, targetFarClip) - depth, 0.0, 1.0);
    return floor(smoothstep(0.01, 0.02, diff) * 2.0) / 2.0;
}

float NeighborNormalEdgeIndicator(float2 UV, float depth, float3 normal, float targetNearClip, float targetFarClip)
{
    float depthDiff = GetDepth(UV, targetNearClip, targetFarClip) - depth;
    float3 neighborNormal = GetNormal(UV);

    // Edge pixels should yield to faces whose normals are closer to the bias normal
    float3 normalEdgeBias = float3(1.0, 1.0, 1.0);
    float normalDiff = dot(normal - neighborNormal, normalEdgeBias);
    float normalIndicator = clamp(smoothstep(-0.01, 0.01, normalDiff), 0.0, 1.0);

    // Only the more shallow pixel should detect the normal edge
    float depthIndicator = clamp(sign(depthDiff * 0.25 + 0.0025), 0.0, 1.0);

    return (1.0 - dot(normal, neighborNormal)) * depthIndicator * normalIndicator;
}

float NormalEdgeIndicator(float2 uvSamples[4], float depth, float3 normal, float targetNearClip, float targetFarClip)
{
    float indicator = 0.0;
    indicator += NeighborNormalEdgeIndicator(uvSamples[0], depth, normal, targetNearClip, targetFarClip);
    indicator += NeighborNormalEdgeIndicator(uvSamples[1], depth, normal, targetNearClip, targetFarClip);
    indicator += NeighborNormalEdgeIndicator(uvSamples[2], depth, normal, targetNearClip, targetFarClip);
    indicator += NeighborNormalEdgeIndicator(uvSamples[3], depth, normal, targetNearClip, targetFarClip);
    return step(0.1, indicator);
}

void Outline_float(float2 UV, float TargetNearClip, float TargetFarClip, float DepthEdgeStrength, float NormalEdgeStrength, out float4 Out)
{
    /*
    float colorSensitivity = 1;
    float4 outlineColor = (1, 1, 1, 1);

    float halfScaleFloor = 0.0;
    float halfScaleCeil = 1.0;
    float2 texel = 1.0 / float2(_CameraColorTexture_TexelSize.z, _CameraColorTexture_TexelSize.w);

    float2 uvSamples[4];
    float depthSamples[4];
    float3 normalSamples[4], colorSamples[4];

    uvSamples[0] = UV - float2(texel.x, texel.y) * halfScaleFloor;
    uvSamples[1] = UV + float2(texel.x, texel.y) * halfScaleCeil;
    uvSamples[2] = UV + float2(texel.x * halfScaleCeil, -texel.y * halfScaleFloor);
    uvSamples[3] = UV + float2(-texel.x * halfScaleFloor, texel.y * halfScaleCeil);

    for (int i = 0; i < 4; i++)
    {
        depthSamples[i] = GetDepth(uvSamples[i], TargetNearClip, TargetFarClip);
        normalSamples[i] = GetNormal(uvSamples[i]);
        colorSamples[i] = SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, uvSamples[i]);
    }

    // Depth
    float depthFiniteDifference0 = depthSamples[1] - depthSamples[0];
    float depthFiniteDifference1 = depthSamples[3] - depthSamples[2];
    float edgeDepth = sqrt(pow(depthFiniteDifference0, 2) + pow(depthFiniteDifference1, 2)) * 100.0;
    float depthThreshold = (1 / DepthEdgeStrength) * depthSamples[0];
    edgeDepth = edgeDepth > depthThreshold ? 1 : 0;

    // Normals
    float3 normalFiniteDifference0 = normalSamples[1] - normalSamples[0];
    float3 normalFiniteDifference1 = normalSamples[3] - normalSamples[2];
    float edgeNormal = sqrt(dot(normalFiniteDifference0, normalFiniteDifference0) + dot(normalFiniteDifference1, normalFiniteDifference1));
    edgeNormal = edgeNormal > (1 / NormalEdgeStrength) ? 1 : 0;

    // Color
    float3 colorFiniteDifference0 = colorSamples[1] - colorSamples[0];
    float3 colorFiniteDifference1 = colorSamples[3] - colorSamples[2];
    float edgeColor = sqrt(dot(colorFiniteDifference0, colorFiniteDifference0) * dot(colorFiniteDifference1, colorFiniteDifference1));
    edgeColor = edgeColor > (1 / colorSensitivity) ? 1 : 0;

    float edge = max(edgeDepth, max(edgeNormal, edgeColor));

    float4 original = SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, uvSamples[0]);
    Out = ((1 - edge) * original) + (edge * lerp(original, outlineColor, outlineColor.a));

    */
    
    float2 texel = 1.0 / float2(_CameraColorTexture_TexelSize.z, _CameraColorTexture_TexelSize.w);
    float2 uvSamples[4];

    uvSamples[0] = UV - float2(texel.x, 0);
    uvSamples[1] = UV + float2(texel.x, 0);
    uvSamples[2] = UV - float2(0, texel.y);
    uvSamples[3] = UV + float2(0, texel.y);

    float depth = 0.0;
    float3 normal = float3(0.0, 0.0, 0.0);

    if (DepthEdgeStrength > 0.0 || NormalEdgeStrength > 0.0)
    {
        depth = GetDepth(UV, TargetNearClip, TargetFarClip);
        normal = GetNormal(UV);
    }

    float depthEdgeIndicator = 0.0;
    if (DepthEdgeStrength > 0.0)
    {
        depthEdgeIndicator = DepthEdgeIndicator(uvSamples, depth, TargetNearClip, TargetFarClip);
    }

    float normalEdgeIndicator = 0.0;
    if (NormalEdgeStrength > 0.0)
    {
        normalEdgeIndicator = NormalEdgeIndicator(uvSamples, depth, normal, TargetNearClip, TargetFarClip);
    }

    float strength = depthEdgeIndicator > 0.0 ? (1.0 - DepthEdgeStrength * depthEdgeIndicator) : (1.0 + NormalEdgeStrength * normalEdgeIndicator);
    // float strength = 1.0 - DepthEdgeStrength * depthEdgeIndicator;
    // float strength = 1.0 + NormalEdgeStrength * normalEdgeIndicator;
    // float strength = 1.0;
    
    // Out = float4(normal, 1.0);
    // Out = float4(depth, depth, depth, 1.0);

    float4 original = SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_CameraColorTexture, UV);
    Out = original * strength;
    // Out = float4(strength, strength, strength, 1);
    // Out = original;
}
#endif
