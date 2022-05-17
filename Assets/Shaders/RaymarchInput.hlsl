#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#include "RaymarchFunctions.hlsl"
#include "RaymarchLighting.hlsl"

TEXTURE2D(_MainTex);            SAMPLER(sampler_MainTex);
TEXTURE2D(_OcclusionMap);       SAMPLER(sampler_OcclusionMap);

float4 _MainTex_ST;
float4 _Tint;
float4 _EmissionColor;
float _Metallic;
float _Smoothness;
float _OcclusionStrength;


struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 texcoord : TEXCOORD0;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv : TEXCOORD0;
    float3 positionWS : TEXCOORD2;
    float3 normalWS : TEXCOORD3;
    float4 tangentWS : TEXCOORD4;
    float3 viewDirWS : TEXCOORD5;
    float4 positionCS : SV_POSITION;
    float3 rayOrigin : TEXCOORD6;
    float3 positionOS : TEXCOORD7;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

half SampleOcclusion(float2 uv)
{
    half occ = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g;
    return LerpWhiteTo(occ, _OcclusionStrength);
}

inline void InitializeSurfaceData(float2 uv, out SurfaceData outSurfaceData)
{
    half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_MainTex, sampler_MainTex));
    outSurfaceData.alpha = albedoAlpha.a;
    outSurfaceData.albedo = albedoAlpha.rgb * _Tint.rgb;

    outSurfaceData.metallic = _Metallic;
    outSurfaceData.specular = half3(0.0h, 0.0h, 0.0h);

    outSurfaceData.smoothness = _Smoothness;
    outSurfaceData.normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), 1);
    outSurfaceData.occlusion = SampleOcclusion(uv);
    outSurfaceData.emission = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));

    outSurfaceData.clearCoatMask = 0.0h;
    outSurfaceData.clearCoatSmoothness = 0.0h;
}


inline void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;

    inputData.positionWS = input.positionWS;

    half3 viewDirWS = SafeNormalize(input.viewDirWS);
    float sgn = input.tangentWS.w;      // should be either +1 or -1
    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
    inputData.normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz));

    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.viewDirectionWS = viewDirWS;
}