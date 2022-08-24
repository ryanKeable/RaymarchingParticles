#define MAX_STEPS 100
#define MAX_DIST 1000
#define SURF_DIST 1e-3 //(0.001)
#define M_PI 3.14159265358979323846264338327950288;
#define M_PI_2 6.28318530718;

uniform float4x4 _4x4Identity;

// https://inspirnathan.com/posts/54-shadertoy-tutorial-part-8/

// TODO:
/*
-breaks when we add another element to the list
- has trouble working out all the Connections
*/


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
    // float h = max(k - abs(d1 - d2), 0.0);
    // return min(d1, d2) - h * h * 0.25 / k;

    float h = clamp(0.5 + 0.5 * (d2 - d1) / k, 0.0, 1.0);
    return lerp(d2, d1, h) - k * h * (1.0 - h);
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

float Sphere(float3 p, float r)
{
    return length(p) - r; // sphere

}

float CappedCone(float3 p, half4x4 transform, float h, float r1, float r2, float3 o)
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

float CappedConeSymXZ(float3 p, half4x4 transform, float h, float r1, float r2, float3 o)
{
    p.xz = abs(p.xz);
    return CappedCone(p, transform, h, r1, r2, o);
}

// I worked out the origin offset all by myself!! YAY!!!
float Cylinder(float3 p, half4x4 transform, float r, float h, float3 o)
{
    p = mul(transform, p);
    float2 d = abs(float2(length(p.xz - o.xz), p.y - o.y)) - float2(r, h);
    return min(max(d.x, d.y), 0.0) + length(max(d, 0.0));
}

float Box(float3 p, float3 b, float3 offset, half4x4 transform)
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