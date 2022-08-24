#include "RaymarchFunctions.hlsl"

uniform float3 _ParticleNodePos[32];
uniform float3 _ParticleNodeScalars[32];
uniform float4 _ConnectionData[32];
uniform float4 _ConnectionSizeData[32];
uniform half4x4 _ConnectionRotationMatrices[32];

const float smoothnessScalarCap = 0.5;

int _ParticleCount = 0;
int _ConnectionCount = 0;

uniform float _UnionSmoothness;

// so currently our origin for each connection is in the middle of the points and we grow outwards
// we neeed to translate the origin to the sphere points their length/2 across the direction vector
// we currently have a rot matrix, not a vector
// we can either use our sphere pos (another 2 vector LUTs -- we cant find the index here without the logic)
// we can either create the vector from the rot matrix, or create the rot matrix from the quaternion and the vector from the quaternion?


// float CalcBridges(float3 p, float4 particle, float3 particleData, inout int runningIndex)
// {
//     // for each connection we have...
//     int connectionCount = particleData.x; // 2
//     float connections;
//     for (int j = 0; j < connectionCount; j++)
//     {
//         float3 pos = particle.xyz;
//         float h = _ConnectionData[runningIndex].x;
//         float r1 = _ConnectionData[runningIndex].y;
//         float r2 = _ConnectionData[runningIndex].z;
//         int index = _ConnectionData[runningIndex].w;
//         int rotIndex = abs(index);
//         int flip = index / rotIndex;

//         half4x4 rotMatrix = _ConnectionRotationMatrices[rotIndex - 1];
//         // rotMatrix = _4x4Identity;
//         float smoothness = particleData.y;

//         float connection = CappedCone(p - pos, flip * transpose(rotMatrix), h, r1, r2, float3(0, h, 0));
//         // float connection = CalcBridgeBetweenParticles(p, startPos, endPos, rotMatrix, float3(h,r1,r2), smoothness);

//         if (connections == 0)
//             connections = connection;
//         else
//             connections = opSmoothUnion(connections, connection, _UnionSmoothness * smoothness); // this makes the previous union fatter

//         runningIndex++;
//     }

//     return connections;
// }

void ParticleNodes(float3 p, inout float scene)
{
    // particle nodes
    // UNITY_UNROLL
    for (int i = 0; i < _ParticleCount; i++)
    {

        float3 pos = _ParticleNodePos[i];
        float scale = _ParticleNodeScalars[i].x;
        float smoothness = _ParticleNodeScalars[i].y;


        if (scale <= 0.001f) continue; // we are miniscule, do not compute

        float node = Sphere(p - pos, scale);

        if (scene == 0)
            scene = node;
        else
            scene = opSmoothUnion(scene, node, smoothness);
    }
}

float CalcBridgeBetweenParticles(float3 p, float3 connetionPos01, float3 connetionPos02, float4 connectionScaleData, half4x4 rotMat)
{
    float temp = .05;
    float h = connectionScaleData.x;
    float r1 = connectionScaleData.y;
    float r2 = connectionScaleData.z;
    float midR = connectionScaleData.w;


    float connection = CappedCone(p - connetionPos01, -transpose(rotMat), h, r1, midR, float3(0, h, 0));
    float flippedConnection = CappedCone(p - connetionPos02, transpose(rotMat), h, r2, midR, float3(0, h, 0));
    return opSmoothUnion(connection, flippedConnection, 0.01);
}


void ParticleNodeConnections(float3 p, inout float scene)
{
    // particle connections
    for (int j = 0; j < _ConnectionCount; j++)
    {
        
        int posIndex01 = (int)_ConnectionData[j].x;
        int posIndex02 = (int)_ConnectionData[j].y;
        float smoothness = _ConnectionData[j].z;

        float3 pos01 = _ParticleNodePos[posIndex01].xyz;
        float3 pos02 = _ParticleNodePos[posIndex02].xyz;

        float4 scaleData = _ConnectionSizeData[j];
        half4x4 rotMatrix = _ConnectionRotationMatrices[j];

        float connection = CalcBridgeBetweenParticles(p, pos01, pos02, scaleData, rotMatrix);
        
        if (scene == 0)
            scene = connection;
        else
            scene = opSmoothUnion(scene, connection, smoothness);
    }
}

float GetDistance(float3 p)
{
    int runningIndex = 0;
    float scene = 0;

    if (_ParticleCount == 0)
        return -1;
    
    #ifndef _DISABLE_CONNECTIONS
        ParticleNodeConnections(p, scene);
    #endif

    // we calc the nodes 2nd as we can reduce the union smoothing based of the # of connections a node has
    ParticleNodes(p, scene);
    
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

half3 RenderRaymarch(float2 uv, float3 rayOrigin, float3 hitPos)
{
    uv -= 0.5;

    half3 col = 0;
    float3 rayDir = normalize(hitPos - rayOrigin);
    float distance = Raymarch(rayOrigin, rayDir);
    
    if (distance > MAX_DIST || distance <= 0) discard;// hit surface

    float3 pos = rayOrigin + rayDir * distance;
    float3 normal = GetNormal(pos);

    //TODO:
    // - make sure we convert to world normals?
    col = normal;
    
    return col;
}