#define MAX_STEPS 100
#define MAX_DIST 1000
#define SURF_DIST 1e-3 //(0.001)
#define M_PI 3.14159265358979323846264338327950288;
#define M_PI_2 6.28318530718;


// https://inspirnathan.com/posts/54-shadertoy-tutorial-part-8/

// TODO:
/*
-breaks when we add another element to the list
- has trouble working out all the Connections
*/
uniform float4x4 _4x4Identity;
uniform half4x4 _ConnectionRotationMatrix[32];

uniform float4 _Particle[32];
uniform float3 _ConnectionPos1[32];
uniform float3 _ConnectionPos2[32];
uniform float4 _ConnectionScale[32];

int _ParticleCount = 0;
int _ConnectionCount = 0;

uniform float _UnionSmoothness;

float dot2(float2 v)
{
    return dot(v, v);
}

float opUnion(float d1, float d2)
{
    return min(d1, d2);
}

float opSmoothUnion(float d1, float d2, float k)
{
    float h = max(k - abs(d1 - d2), 0.0);
    return min(d1, d2) - h * h * 0.25 / k;

    // float h = clamp(0.5 + 0.5 * (d2 - d1) / k, 0.0, 1.0);
    // return lerp(d2, d1, h) - k * h * (1.0 - h);

}

float opIntersection(float d1, float d2)
{
    return max(d1, d2);
}

float opSmoothIntersection(float d1, float d2, float k)
{
    float h = clamp(0.5 - 0.5 * (d2 - d1) / k, 0.0, 1.0);
    return lerp(d2, d1, h) + k * h * (1.0 - h);
}

float opSubtraction(float d1, float d2)
{
    return max(-d1, d2);
}

float opSmoothSubtraction(float d1, float d2, float k)
{
    float h = clamp(0.5 - 0.5 * (d2 + d1) / k, 0.0, 1.0);
    return lerp(d2, -d1, h) + k * h * (1.0 - h);
}


float Torus(float3 p)
{
    return length(float2(length(p.xz) - 0.5, p.y)) - 0.1;
}

float Sphere(float3 p, float r)
{
    return length(p) - r; // sphere

}

float CappedCone(float3 p, float3x3 transform, float h, float r1, float r2, float3 o)
{
    p = mul(transform, p);
    float2 q = float2(length(p.xz - o.xz), p.y - o.y);
    float2 k1 = float2(r2, h);
    float2 k2 = float2(r2 - r1, 2.0 * h);
    float2 ca = float2(q.x - min(q.x, (q.y < 0.0)?r1 : r2), abs(q.y) - h);
    float2 cb = q - k1 + k2 * clamp(dot(k1 - q, k2) / dot2(k2), 0.0, 1.0);
    float s = (cb.x < 0.0 && ca.y < 0.0) ? - 1.0 : 1.0;
    return s * sqrt(min(dot2(ca), dot2(cb)));
}

float CappedConeSymXZ(float3 p, float3x3 transform, float h, float r1, float r2, float3 o)
{
    p.xz = abs(p.xz);
    return CappedCone(p, transform, h, r1, r2, o);
}

// I worked out the origin offset all by myself!! YAY!!!
float Cylinder(float3 p, float3x3 transform, float r, float h, float3 o)
{
    p = mul(transform, p);
    float2 d = abs(float2(length(p.xz - o.xz), p.y - o.y)) - float2(r, h);
    return min(max(d.x, d.y), 0.0) + length(max(d, 0.0));
}

float Box(float3 p, float3 b, float3 offset, float3x3 transform)
{
    p = mul((p - offset), transform);
    float3 q = abs(p) - b;
    float d = length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0);
    return d;
}

float VectorAngleRadians(float3 a, float3 b)
{
    float p = dot(a, b);
    float d = length(a) * length(b);
    float t = p / d;
    return acos(t);
}

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

    float smoothness = midR / (r1 + r2) * 0.5;
    float connection = CappedCone(p - connectionP1, -transpose(rotMat), h, r1, midR, float3(0, h, 0));
    float flippedConnection = CappedCone(p - connectionP2, transpose(rotMat), h, r2, midR, float3(0, h, 0));
    return opSmoothUnion(connection, flippedConnection, _UnionSmoothness * smoothness);
}

float GetDistance(float3 p)
{
    float dist = 0;

    if (_ParticleCount == 0)
    {
        return dist;
    }

    float spheres = Sphere(p - _Particle[0].xyz, _Particle[0].w);

    if (_ParticleCount == 1)
    {
        dist = spheres;
        return dist;
    }

    UNITY_UNROLL
    for (int i = 1; i < _ParticleCount; i++)
    {
        float sphere = Sphere(p - _Particle[i].xyz, _Particle[i].w);
        spheres = opSmoothUnion(sphere, spheres, _UnionSmoothness);
    }

    if (_ConnectionCount == 0)
    {
        dist = spheres;
        return dist;
    }

    float connections = CalcBridgeBetweenParticles(p, _ConnectionPos1[0].xyz, _ConnectionPos2[0].xyz, _ConnectionRotationMatrix[0], _ConnectionScale[0]);

    UNITY_UNROLL
    for (int j = 1; j < _ConnectionCount; j++)
    {
        float bridge = CalcBridgeBetweenParticles(p, _ConnectionPos1[j].xyz, _ConnectionPos2[j].xyz, _ConnectionRotationMatrix[j], _ConnectionScale[j]);
        connections = opSmoothUnion(bridge, connections, _UnionSmoothness);
    }
    
    // float intersections = opIntersection(connections, spheres);
    // return intersections;

    // float subtractions = opSmoothSubtraction(connections, spheres, _UnionSmoothness);
    // return subtractions;

    dist = opSmoothUnion(spheres, connections, _UnionSmoothness); // this division is causing some errors around the visual edges?
    return dist;
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