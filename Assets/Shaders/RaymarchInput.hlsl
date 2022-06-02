#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
// #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl"
// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


// #include "RaymarchLighting.hlsl"

TEXTURE2D(_MainTex);            SAMPLER(sampler_MainTex);

float4 _MainTex_ST;

struct Attributes
{
    float4 positionOS : POSITION;
    float2 texcoord : TEXCOORD0;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv : TEXCOORD0;
    float3 positionWS : TEXCOORD2;
    float4 positionCS : SV_POSITION;
    float3 rayOrigin : TEXCOORD6;
    float3 positionOS : TEXCOORD7;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};
