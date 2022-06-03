#include "RaymarchFunctions.hlsl"

uniform float4 _Particles[32];
uniform float4 _ParticleData[32];
uniform float _ConnectionLengths[32];
uniform half4 _ConnectionData[32];
uniform half4x4 _ConnectionRotationMatrices[32];

uniform float3 _ConnectionPos01[32];
uniform float3 _ConnectionPos02[32];

int _ParticleCount = 0;
int _ConnectionCount = 0;

uniform float _UnionSmoothness;

// so currently our origin for each connection is in the middle of the points and we grow outwards
// we neeed to translate the origin to the sphere points their length/2 across the direction vector
// we currently have a rot matrix, not a vector
// we can either use our sphere pos (another 2 vector LUTs -- we cant find the index here without the logic)
// we can either create the vector from the rot matrix, or create the rot matrix from the quaternion and the vector from the quaternion?
float CalcBridgeBetweenParticles(float3 p, float3 connectionP1, float3 connectionP2, half4x4 rotMat, float3 scales, float smoothness)
{
    // float temp = scales.x * (1 - _UnionSmoothness * scales.w) / scales.x * 2; //can factor upto 0.8
    float h = scales.x;
    float r1 = scales.y;
    float r2 = scales.z;
    float midR = min(r1, r2) * 0.5f; // scale.w

    float connection = CappedCone(p - connectionP1, -transpose(rotMat), h, r1, midR, float3(0, h, 0));
    float flippedConnection = CappedCone(p - connectionP2, transpose(rotMat), h, r2, midR, float3(0, h, 0));
    return opSmoothUnion(connection, flippedConnection, _UnionSmoothness * smoothness);
}

float CalcBridges(float3 p, float4 particle, float3 particleData, inout int runningIndex)
{
    // for each connection we have...
    int connectionCount = particleData.x; // 2
    float connections;
    for (int j = 0; j < connectionCount; j++)
    {
        float3 pos = particle.xyz;
        float h = _ConnectionData[runningIndex].x;
        float r1 = _ConnectionData[runningIndex].y;
        float r2 = _ConnectionData[runningIndex].z;
        int index = _ConnectionData[runningIndex].w;
        int rotIndex = abs(index);
        int flip = index / rotIndex;
        
        half4x4 rotMatrix = _ConnectionRotationMatrices[rotIndex - 1];
        // rotMatrix = _4x4Identity;
        float smoothness = particleData.y;

        float connection = CappedCone(p - pos, flip * transpose(rotMatrix), h, r1, r2, float3(0, h, 0));
        // float connection = CalcBridgeBetweenParticles(p, startPos, endPos, rotMatrix, float3(h,r1,r2), smoothness);

        if (connections == 0)
            connections = connection;
        else
            connections = opSmoothUnion(connections, connection, _UnionSmoothness * smoothness); // this makes the previous union fatter
        
        runningIndex++;
    }

    return connections;
}


float GetDistance(float3 p)
{
    int runningIndex = 0;
    float scene = 0;

    if (_ParticleCount == 0)
        return -1;

    // UNITY_UNROLL
    for (int i = 0; i < _ParticleCount; i++)
    {
        float sphere = Sphere(p - _Particles[i].xyz, _Particles[i].w);
        
        if (scene == 0)
            scene = sphere;
        else
            scene = opSmoothUnion(scene, sphere, _UnionSmoothness * _Particles[i].w);
        
        if (_ParticleData[i].x == 0) continue; // do not calcc bridges if we have none

        float connections = CalcBridges(p, _Particles[i], _ParticleData[i], runningIndex); // generates the connections and fuses them together
        scene = opSmoothUnion(scene, connections, _UnionSmoothness * _Particles[i].w); // this connects the sphere to the connections

    }

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

    if (distance > MAX_DIST || distance < 0) discard;// hit surface

    float3 pos = rayOrigin + rayDir * distance;
    float3 normal = GetNormal(pos);

    //TODO:
    // - make sure we convert to world normals?
    col = normal;

    return col;
}