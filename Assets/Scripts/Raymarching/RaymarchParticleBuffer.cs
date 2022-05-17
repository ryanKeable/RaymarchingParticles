using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class RaymarchParticleBuffer : MonoBehaviour
{
    public List<Vector4> particles = new List<Vector4>();
    public float unionSmoothness;
    public Material renderMat;

    private void Update()
    {
        SetMaterialProps();
    }

    private void OnValidate()
    {
        SetMaterialProps();
    }

    // this only renders correctly after a re-compile?
    public void SetMaterialProps()
    {
        renderMat.SetVectorArray("_Particle", particles.ToArray());
        renderMat.SetInt("_ParticleCount", particles.Count);
        renderMat.SetFloat("_UnionSmoothness", unionSmoothness);

        SetPerParticleBridgeData();
    }

    public float VectorAngleRadians(Vector3 a, Vector3 b)
    {
        float p = Vector3.Dot(a, b);
        float d = Vector3.Magnitude(a) * Vector3.Magnitude(b);
        float t = p / d;
        return Mathf.Acos(t);
    }

    private void SetPerParticleBridgeData()
    {
        Matrix4x4[] particleBridgeRotMatrix = new Matrix4x4[particles.Count];
        Vector4[] particleBridgeTranslationScale = new Vector4[particles.Count];

        // need to skip over the first particle
        for (int i = 1; i < particles.Count; i++)
        {
            CalcParticleBridgeTransforms(particles.ToArray(), i, out Matrix4x4 rotMatrix, out Vector4 translateAndScale);
            particleBridgeRotMatrix[i - 1] = rotMatrix;
            particleBridgeTranslationScale[i - 1] = translateAndScale;

        }

        renderMat.SetMatrixArray("_BridgeRotationMatrix", particleBridgeRotMatrix);
        renderMat.SetVectorArray("_BridgeData", particleBridgeTranslationScale);
    }

    private void CalcParticleBridgeTransforms(Vector4[] particles, int index, out Matrix4x4 rotMatrix, out Vector4 translateAndScale)
    {
        float dist = 0;
        float closestDist = Mathf.Infinity;
        int closestParticleIndex = index - 1;

        for (int i = 0; i < particles.Length; i++)
        {
            // need to ignore this particle
            if (i == index) continue;

            dist = Vector3.Distance(particles[index], particles[i]);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestParticleIndex = i;
            }
        }


        Vector3 a = new Vector3(particles[index].x, particles[index].y, particles[index].z);
        Vector3 b = new Vector3(particles[closestParticleIndex].x, particles[closestParticleIndex].y, particles[closestParticleIndex].z);

        Vector3 dir = Vector3.Normalize(a - b);
        closestDist /= 2;
        Vector3 offset = a - dir * closestDist;
        Quaternion q = Quaternion.FromToRotation(Vector3.up, dir);

        rotMatrix = Matrix4x4.TRS(Vector3.zero, q, Vector3.one);
        translateAndScale = new Vector4(offset.x, offset.y, offset.z, closestDist);
    }
}