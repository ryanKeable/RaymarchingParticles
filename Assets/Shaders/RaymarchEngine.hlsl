#include "RaymarchFunctions.hlsl"

uniform half4x4 _ConnectionRotationMatrix[32];

uniform float4 _Particle[32];
uniform float4 _ParticleConnections[32];
uniform half4 _ConnectionScale[32];

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
float CalcBridgeBetweenParticles(float3 p, float3 connectionP1, float3 connectionP2, half4x4 rotMat, float4 scale)
{
    float h = scale.x;
    float r1 = scale.y;
    float r2 = scale.z;
    float midR = scale.w; //abs((r1 - r2) * 0.5) -- this was correct but we can compute it on the CPU

    // float smoothness = midR / (r1 + r2) * 0.5;
    float connection = CappedCone(p - connectionP1, -transpose(rotMat), h, r1, midR, float3(0, h, 0));
    float flippedConnection = CappedCone(p - connectionP2, transpose(rotMat), h, r2, midR, float3(0, h, 0));
    return opSmoothUnion(connection, flippedConnection, _UnionSmoothness);
}

// float CalcBridges(int i, float scene, float3 p, inout int runningIndex)
// {
//     // for each connection we have...
//     int connections = _ParticleConnections[i].x;
//     if (connections == 0) return scene;

//     int index01 = _ParticleConnections[i].y;
//     int index02 = _ParticleConnections[i].z;
//     int index03 = _ParticleConnections[i].w;


//     for (int j = 1; j < connections + 1; j++)
//     {
//         int particleIndex = _ParticleConnections[i][j];
//         float bridge = CalcBridgeBetweenParticles(p, _Particle[i].xyz, _Particle[particleIndex].xyz, _ConnectionRotationMatrix[runningIndex], _ConnectionScale[runningIndex]);
//         scene = opSmoothUnion(bridge, scene, _UnionSmoothness);
//         runningIndex++;
//     }

//     // surely there is a better way than this
//     // if (connections == 0) return scene;
//     // if (connections == 1)
//     // {
//     //     float bridge = CalcBridgeBetweenParticles(p, _Particle[i].xyz, _Particle[_ParticleConnections[i].y].xyz, _ConnectionRotationMatrix[runningIndex], _ConnectionScale[runningIndex]);
//     //     scene = opSmoothUnion(bridge, scene, _UnionSmoothness);
//     //     runningIndex++;
//     // }
//     // if (connections == 2)
//     // {
//     //     float bridge01 = CalcBridgeBetweenParticles(p, _Particle[i].xyz, _Particle[index01].xyz, _ConnectionRotationMatrix[runningIndex], _ConnectionScale[runningIndex]);
//     //     scene = opSmoothUnion(bridge01, scene, _UnionSmoothness);
//     //     runningIndex++;
//     //     float bridge02 = CalcBridgeBetweenParticles(p, _Particle[i].xyz, _Particle[index02].xyz, _ConnectionRotationMatrix[runningIndex], _ConnectionScale[runningIndex]);
//     //     scene = opSmoothUnion(bridge02, scene, _UnionSmoothness);
//     //     runningIndex++;
//     // }
//     // if (connections == 3)
//     // {
//     //     float bridge01 = CalcBridgeBetweenParticles(p, _Particle[i].xyz, _Particle[index01].xyz, _ConnectionRotationMatrix[runningIndex], _ConnectionScale[runningIndex]);
//     //     scene = opSmoothUnion(bridge01, scene, _UnionSmoothness);
//     //     runningIndex++;
//     //     float bridge02 = CalcBridgeBetweenParticles(p, _Particle[i].xyz, _Particle[index02].xyz, _ConnectionRotationMatrix[runningIndex], _ConnectionScale[runningIndex]);
//     //     scene = opSmoothUnion(bridge02, scene, _UnionSmoothness);
//     //     runningIndex++;
//     //     float bridge03 = CalcBridgeBetweenParticles(p, _Particle[i].xyz, _Particle[index02].xyz, _ConnectionRotationMatrix[runningIndex], _ConnectionScale[runningIndex]);
//     //     scene = opSmoothUnion(bridge03, scene, _UnionSmoothness);
//     //     runningIndex++;
//     // }

//     return scene;
// }

float GetDistance(float3 p)
{
    float dist = 0;
    int runningIndex = 0;

    if (_ParticleCount == 0)
    {
        return -1;
    }

    float scene = Sphere(p - _Particle[0].xyz, _Particle[0].w);

    // float bridge = CalcBridgeBetweenParticles(p, _Particle[0].xyz, _Particle[_ParticleConnections[0].y].xyz, _ConnectionRotationMatrix[0], _ConnectionScale[0]);
    // scene = opSmoothUnion(bridge, scene, _UnionSmoothness);

    // UNITY_UNROLL
    for (int i = 1; i < _ParticleCount; i++)
    {
        float sphere = Sphere(p - _Particle[i].xyz, _Particle[i].w);
        // scene = opUnion(sphere, scene);
        scene = opSmoothUnion(sphere, scene, _UnionSmoothness / 2);
    }


    if (_ParticleCount > 0)
    {
        float connections = CalcBridgeBetweenParticles(p, _ConnectionPos01[0].xyz, _ConnectionPos02[0].xyz, _ConnectionRotationMatrix[0], _ConnectionScale[0]);

        // UNITY_UNROLL
        for (int j = 1; j < _ConnectionCount; j++)
        {
            float bridge = CalcBridgeBetweenParticles(p, _ConnectionPos01[j].xyz, _ConnectionPos02[j].xyz, _ConnectionRotationMatrix[j], _ConnectionScale[j]);
            // connections = opUnion(bridge, connections);
            connections = opSmoothUnion(bridge, connections, _UnionSmoothness / 2);
        }
        
        scene = opSmoothUnion(scene, connections, _UnionSmoothness); // this division is causing some errors around the visual edges?

    }

    // dist = opSmoothUnion(scene, connections, _UnionSmoothness); // this division is causing some errors around the visual edges?
    // dist = opSmoothUnion(scene, scene, _UnionSmoothness); // this division is causing some errors around the visual edges?
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