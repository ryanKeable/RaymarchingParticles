#define MAX_STEPS 100
#define MAX_DIST 1000
#define SURF_DIST 1e-3 //(0.001)
#define M_PI 3.14159265358979323846264338327950288;
#define M_PI_2 6.28318530718;


// https://inspirnathan.com/posts/54-shadertoy-tutorial-part-8/


float3x3 DegreeToRadians(float degrees)
{
    float pi = M_PI;
    return degrees * pi / 180.0;
}

float3 slerp(float3 start, float3 end, float percent)
{
    // Dot product - the cosine of the angle between 2 vectors.
    float p = dot(start, end);
    // Clamp it to be in the range of Acos()
    // This may be unnecessary, but floating point
    // precision can be a fickle mistress.
    p = clamp(p, -1.0, 1.0);
    // Acos(dot) returns the angle between start and end,
    // And multiplying that by percent returns the angle between
    // start and the final result.
    float theta = acos(p) * percent;
    float3 RelativeVec = normalize(end - start * p); // Orthonormal basis
    // The final result.
    return((start * cos(theta)) + (RelativeVec * sin(theta)));
}

float3x3 rotate_x(float a)
{
    float sa = sin(a);
    float ca = cos(a);
    return float3x3(float3(1., .0, .0), float3(.0, ca, sa), float3(.0, -sa, ca));
}

float3x3 rotate_y(float a)
{
    float sa = sin(a);
    float ca = cos(a);
    return float3x3(float3(ca, .0, sa), float3(.0, 1., .0), float3(-sa, .0, ca));
}

float3x3 rotate_z(float a)
{
    float sa = sin(a);
    float ca = cos(a);
    return float3x3(float3(ca, sa, .0), float3(-sa, ca, .0), float3(.0, .0, 1.));
}

float3x3 rotate_xyz(float3x3 x, float3x3 y, float3x3 z)
{
    return mul(mul(x, y), z);
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

// should we be using capped cones instead where each end can scale according the scale of their corresponding sphere
float Cylinder(float3 p, float3x3 transform, float r, float h)
{
    p = mul(transform, p);
    float2 d = abs(float2(length(p.xz), p.y)) - float2(r, h);
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

uniform float4 _Particle[8];
uniform float _UnionSmoothness;
uniform float4x4 _BridgeRotationMatrix[8];
uniform float4 _BridgeData[8];
int _ParticleCount;

float GetDistance(float3 p)
{
    float dist = 0;

    // UNITY_UNROLL
    for (int i = 0; i < _ParticleCount; i++)
    {
        float sphere = Sphere(p - _Particle[i].xyz, _Particle[i].w);
        
        if (i > 0)
        {
            // position the cylinder halfway between this sphere and the last
            // rotate the cylinder towards our sphere
            
            float3 dir = _Particle[i].xyz - _Particle[i - 1].xyz;
            float d = distance(_Particle[i].xyz, _Particle[i - 1].xyz);
            float3 o = _Particle[i].xyz - normalize(dir) * (d / 2);
            
            float bridge = Cylinder(p - _BridgeData[i - 1].xyz, transpose(_BridgeRotationMatrix[i - 1]), .025, _BridgeData[i - 1].w);

            float blend = opSmoothUnion(sphere, bridge, _UnionSmoothness);
            dist = opSmoothUnion(blend, dist, _UnionSmoothness);
        }
        else
        {
            dist = sphere;
        }
    }
    
    return dist;
}

float3 GetNormal(float3 pos) // constructred normal

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