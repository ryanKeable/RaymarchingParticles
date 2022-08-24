
#include "RaymarchFunctions.hlsl"

uniform float4 _Sphere;
uniform float3 _CylinderPos;
uniform float3 _CylinderScale;
uniform float _CylinderYOffset;
uniform float _CylinderScalar;
uniform float _SphereScalar;
uniform float _Smoothness;

float GetDistance(float3 p)
{
    float sphere = Sphere(p - _Sphere.xyz, _Sphere.w);

    float h = _CylinderScale.x * _CylinderScalar;

    float intersectionRadius = _Sphere.w - _CylinderYOffset;
    intersectionRadius = min(intersectionRadius, _Sphere.w * 0.8);
    intersectionRadius = max(intersectionRadius, _Sphere.w * 0.2);


    float r1 = intersectionRadius * (1 + _CylinderYOffset * _CylinderScale.y * 8) * _CylinderScalar;
    float r2 = intersectionRadius * (1 + _CylinderYOffset * _CylinderScale.z * 8) * 0.5 * _CylinderScalar;
    float smoothness = min(_Smoothness * _CylinderScalar * _SphereScalar, _Smoothness);
    
    #ifndef _DISABLE_CONNECTIONS
        float cylinder01 = CappedCone(p - _Sphere.xyz, _4x4Identity, h, r1, r2, float3(0, h * 1 + _CylinderYOffset, 0));
        float scene = opSmoothUnion(sphere, cylinder01, smoothness);
        float cylinder02 = CappedCone(p - _Sphere.xyz, _4x4Identity, h, r2, r1, float3(0, -h * 1 - _CylinderYOffset, 0));
        scene = opSmoothUnion(scene, cylinder02, smoothness);
    #else
        float scene = sphere;
    #endif
    
    // scene = opSmoothUnion(scene, cylinder02, _Smoothness);
    return scene;
}

// constructred normal
float3 GetNormal(float3 pos)
{
    float2 epsilon = float2(1e-2, 0);

    float3 normal = GetDistance(pos) - float3(
        GetDistance(pos - epsilon.xyy),
        GetDistance(pos - epsilon.yxy),
        GetDistance(pos - epsilon.yyx)
    );

    return normalize(normal);
}

float Raymarch(float3 rayOrigin, float3 rayDir)
{
    float distanceFromOrigin = 0.0;
    float distanceFromSurf = 0.0;

    for (int i = 0; i < MAX_STEPS; i++)
    {
        float3 p = rayOrigin + distanceFromOrigin * rayDir;
        distanceFromSurf = GetDistance(p);
        distanceFromOrigin += distanceFromSurf;

        if (distanceFromSurf < SURF_DIST || distanceFromOrigin > MAX_DIST) break;
    }

    return distanceFromOrigin;
}

float3 RenderRaymarch(float2 uv, float3 rayOrigin, float3 hitPos)
{
    uv -= 0.5;

    half3 col = 0;
    float3 rayDir = normalize(hitPos - rayOrigin);
    float distance = Raymarch(rayOrigin, rayDir);

    if (distance > MAX_DIST || distance == 0) discard;// hit surface

    float3 pos = rayOrigin + rayDir * distance;
    float3 normal = GetNormal(pos);

    //TODO:
    // - make sure we convert to world normals?
    col = normal;

    return col;
}