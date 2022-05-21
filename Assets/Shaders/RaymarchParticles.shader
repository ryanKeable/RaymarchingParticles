Shader "Raymarch/RaymarchParticles"
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
        ZWrite On
        ZTest less
        Cull Back
        

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vertex
            #pragma fragment fragment

            #include "RaymarchInput.hlsl"
            
            Varyings vertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                float3 viewDirWS = GetCameraPositionWS() - vertexInput.positionWS;

                output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);

                output.normalWS = NormalizeNormalPerVertex(normalInput.normalWS);
                real sign = input.tangentOS.w * GetOddNegativeScale();
                float4 tangentWS = float4(normalInput.tangentWS.xyz, sign);

                output.viewDirWS = viewDirWS;
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;

                output.rayOrigin = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos.xyz, 1)).xyz;
                output.positionOS = input.positionOS.xyz;

                return output;
            }

            half4 fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                SurfaceData surfaceData;
                InitializeSurfaceData(input.uv, surfaceData);

                InputData inputData;
                InitializeInputData(input, surfaceData.normalTS, inputData);

                half3 color = RenderRaymarch(input.uv, input.rayOrigin, input.positionOS);

                return half4(color, 1);
            }

            ENDHLSL

        }
    }
}