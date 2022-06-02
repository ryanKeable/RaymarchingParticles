Shader "Raymarch/RaymarchParticlesDebug"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" { }
        _EnvironmentLUC ("_Environment", CUBE) = "white" { }
        _Tint ("Albedo", Color) = (1, 1, 1, 1)
        _Smoothness ("Smoothness", Range(0, 1)) = .5
        _Metallic ("Metallic", Range(0, 1)) = .5
        _Occlusion ("Occlusion", Range(0, 1)) = 1
        [HDR] _Emission ("Emission", Color) = (0, 0, 0, 0)
    }
    SubShader
    {
        Name "Raymarch_Standard"

        Tags { "RenderPipeline" = "UniversalPipeline" }
        

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vertex
            #pragma fragment fragment

            #include "RaymarchInput.hlsl"
            #include "RaymarchEngineDebug.hlsl"
            
            Varyings vertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

                output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;

                output.rayOrigin = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos.xyz, 1)).xyz;
                output.positionOS = input.positionOS.xyz;

                return output;
            }

            half4 fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 color = RenderRaymarch(input.uv, input.rayOrigin, input.positionOS);

                return half4(color, 1);
            }

            ENDHLSL

        }
    }
}