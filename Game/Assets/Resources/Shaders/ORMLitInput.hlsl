#ifndef UNIVERSAL_ORM_LIT_INPUT_INCLUDED
#define UNIVERSAL_ORM_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

#if defined(_DETAIL_MULX2) || defined(_DETAIL_SCALED)
    #define _DETAIL
#endif

// NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _DetailAlbedoMap_ST;
    half4 _BaseColor;
    half4 _SpecColor;
    half4 _EmissionColor;
    half _Cutoff;
    half _Smoothness;
    half _Metallic;
    half _BumpScale;
    half _Parallax;
    half _OcclusionStrength;
    half _ClearCoatMask;
    half _ClearCoatSmoothness;
    half _DetailAlbedoMapScale;
    half _DetailNormalMapScale;
    half _Surface;
    
    // ORM specific properties
    half _RoughnessScale;
    half _MetallicScale;
    
    // Self Emissive properties
    half4 _SelfEmissionColor;
    half _EmissionThreshold;
    half _EmissionSource;
    half _EmissionChannel;
    half _EmissionMultiplier;
CBUFFER_END

// Main ORM texture
TEXTURE2D(_ORMMap);             SAMPLER(sampler_ORMMap);

///////////////////////////////////////////////////////////////////////////////
//                      Material Property Helpers                           //
///////////////////////////////////////////////////////////////////////////////

// Custom ORM sampling function
half4 SampleORMMap(float2 uv)
{
    return SAMPLE_TEXTURE2D(_ORMMap, sampler_ORMMap, uv);
}

// Override the standard sampling functions to use ORM data
half SampleOcclusion(float2 uv)
{
    half4 orm = SampleORMMap(uv);
    half occ = orm.r;  // Red channel = Occlusion
    return LerpWhiteTo(occ, _OcclusionStrength);
}

half4 SampleMetallicSpecGloss(float2 uv, half alpha)
{
    half4 orm = SampleORMMap(uv);
    
    // Green channel = Roughness (convert to smoothness)
    half roughness = orm.g * _RoughnessScale;
    half smoothness = 1.0 - roughness;
    
    // Blue channel = Metallic
    half metallic = orm.b * _MetallicScale;
    
    half4 specGloss;
    specGloss.rgb = half3(metallic, metallic, metallic);
    specGloss.a = smoothness;

    return specGloss;
}

// SampleAlbedoAlpha is already defined in SurfaceInput.hlsl - we'll use the standard one

// Self-emissive sampling function
half3 SampleSelfEmission(float2 uv)
{
    half channelValue = 0.0;
    
    // Choose source texture
    if (_EmissionSource < 0.5) // Albedo texture
    {
        half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
        if (_EmissionChannel < 0.5) channelValue = albedo.r;      // Red
        else if (_EmissionChannel < 1.5) channelValue = albedo.g; // Green  
        else if (_EmissionChannel < 2.5) channelValue = albedo.b; // Blue
        else channelValue = albedo.a;                              // Alpha
    }
    else if (_EmissionSource < 1.5) // ORM texture
    {
        half4 orm = SampleORMMap(uv);
        if (_EmissionChannel < 0.5) channelValue = orm.r;      // Red (Occlusion)
        else if (_EmissionChannel < 1.5) channelValue = orm.g; // Green (Roughness)
        else if (_EmissionChannel < 2.5) channelValue = orm.b; // Blue (Metallic)
        else channelValue = orm.a;                              // Alpha
    }
    else // Normal texture
    {
        half4 normal = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv);
        if (_EmissionChannel < 0.5) channelValue = normal.r;      // Red
        else if (_EmissionChannel < 1.5) channelValue = normal.g; // Green
        else if (_EmissionChannel < 2.5) channelValue = normal.b; // Blue
        else channelValue = normal.a;                              // Alpha
    }
    
    // Apply threshold and multiplier
    half emissionMask = step(_EmissionThreshold, channelValue);
    half emissionIntensity = (channelValue - _EmissionThreshold) / (1.0 - _EmissionThreshold);
    emissionIntensity = saturate(emissionIntensity) * emissionMask * _EmissionMultiplier;
    
    return _SelfEmissionColor.rgb * emissionIntensity;
}

inline void InitializeStandardLitSurfaceData(float2 uv, out SurfaceData outSurfaceData)
{
    // Sample albedo from separate diffuse texture
    half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    outSurfaceData.alpha = Alpha(albedoAlpha.a, _BaseColor, _Cutoff);
    outSurfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb;

    // Get PBR properties from ORM map
    half4 specGloss = SampleMetallicSpecGloss(uv, albedoAlpha.a);
    outSurfaceData.metallic = specGloss.r;
    outSurfaceData.specular = half3(0.0, 0.0, 0.0);
    outSurfaceData.smoothness = specGloss.a;
    
    // Normal map
    outSurfaceData.normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
    
    // Occlusion from ORM map
    outSurfaceData.occlusion = SampleOcclusion(uv);
    
    // Self-emissive system
    outSurfaceData.emission = SampleSelfEmission(uv);
    
    // No clear coat
    outSurfaceData.clearCoatMask = half(0.0);
    outSurfaceData.clearCoatSmoothness = half(0.0);
}

#endif // UNIVERSAL_ORM_LIT_INPUT_INCLUDED
