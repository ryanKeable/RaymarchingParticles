TEXTURECUBE(_EnvironmentLUC);           SAMPLER(sampler_EnvironmentLUC);

half3 EnvironmentReflection(half3 reflectVector, half perceptualRoughness, half occlusion)
{
    half mip = PerceptualRoughnessToMipmapLevel(perceptualRoughness);
    half4 encodedIrradiance = SAMPLE_TEXTURECUBE_LOD(_EnvironmentLUC, sampler_EnvironmentLUC, reflectVector, mip);

//TODO:DOTS - we need to port probes to live in c# so we can manage this manually.
// #if defined(UNITY_USE_NATIVE_HDR) || defined(UNITY_DOTS_INSTANCING_ENABLED)
//     half3 irradiance = encodedIrradiance.rgb;
// #else
//     half3 irradiance = DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);
// #endif

    half3 irradiance = encodedIrradiance.rgb;

    return irradiance * occlusion;
}

half3 GlobalIllumination(BRDFData brdfData, half occlusion, half3 normalWS, half3 viewDirectionWS)
{
    half3 reflectVector = reflect(-viewDirectionWS, normalWS);
    half NoV = saturate(dot(normalWS, viewDirectionWS));
    half fresnelTerm = Pow4(1.0 - NoV);

    half3 indirectDiffuse = occlusion;
    half3 indirectSpecular = EnvironmentReflection(reflectVector, brdfData.perceptualRoughness, occlusion);

    half3 color = EnvironmentBRDF(brdfData, indirectDiffuse, indirectSpecular, fresnelTerm);

    return color;
}

float4 RaymarchFragmentPBR(InputData inputData, SurfaceData surfaceData, float2 uv, float3 rayOrigin, float3 hitPos)
{

    // BRDFData brdfData = (BRDFData)0;

    // // NOTE: can modify alpha
    // InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);


    // half3 color = GlobalIllumination(brdfData, surfaceData.occlusion, inputData.normalWS, inputData.viewDirectionWS);
    // color += surfaceData.emission;

    half3 color = RenderRaymarch(uv, rayOrigin, hitPos);
    // float3 camera = 
    
    // float3 ray = RayCast()

    return half4(color, surfaceData.alpha);
}