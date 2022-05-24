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
uniform float4x4 _ConnectionRotationMatrix[32];

uniform float4 _Particle[32];
uniform float3 _ConnectionPos[32];
uniform float3 _ConnectionScale[32];

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
    float h = clamp(0.5 + 0.5 * (d2 - d1) / k, 0.0, 1.0);
    return lerp(d2, d1, h) - k * h * (1.0 - h);
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


float GetDistance(float3 p)
{
    if (_ParticleCount < 3) return 1;

    float dist = 0;

    float spheres = Sphere(p - _Particle[0].xyz, _Particle[0].w);

    float h = _ConnectionScale[0].x;
    float r1 = _ConnectionScale[0].y;
    float r2 = _ConnectionScale[0].z;
    float connections = CappedCone(p - _ConnectionPos[0].xyz, transpose(_ConnectionRotationMatrix[0]), h, r1, r2, float3(0, h, 0));

    UNITY_UNROLL
    for (int i = 1; i < _ParticleCount; i++)
    {
        float sphere = Sphere(p - _Particle[i].xyz, _Particle[i].w);
        spheres = opSmoothUnion(sphere, spheres, _UnionSmoothness);
    }

    dist = spheres;
    return dist;
    
    UNITY_UNROLL
    for (int j = 1; j < _ConnectionCount; j++)
    {

        float h = _ConnectionScale[j].x;
        float r1 = _ConnectionScale[j].y;
        float r2 = _ConnectionScale[j].z;
        float connection = CappedCone(p - _ConnectionPos[j].xyz, transpose(_ConnectionRotationMatrix[j]), h, r1, r2, float3(0, h, 0));
        
        connections = opSmoothUnion(connection, connections, _UnionSmoothness);
    }
    


    dist = opSmoothUnion(spheres, connections, _UnionSmoothness); // this division is causing some errors around the visual edges

    
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

    if (distance > MAX_DIST) discard;// hit surface

    float3 pos = rayOrigin + rayDir * distance;
    float3 normal = GetNormal(pos);

    //TODO:
    // - make sure we convert to world normals?
    col = normal;

    return col;
}