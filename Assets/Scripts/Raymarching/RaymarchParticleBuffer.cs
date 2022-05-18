using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// TODO:
/*
"Property (_BridgeData) exceeds previous array size (7 vs 6). Cap to previous size. Restart Unity to recreate the arrays.
UnityEngine.Material:SetVectorArray (string,UnityEngine.Vector4[])"

 -- should we just fill the array to the max but keep the count to the particles we are actually using?
 
*/


[ExecuteInEditMode]
public class RaymarchParticleBuffer : MonoBehaviour
{
    public List<Vector4> particles = new List<Vector4>();
    public float unionSmoothness;
    public Material renderMat;

    private List<Vector4> _particleConnectionTranslationScale;
    private List<Matrix4x4> _particleBridgeRotMatrix;
    private List<float> _distances;

    private int _count;

    public Vector4[] ParticleConnections { get => _particleConnectionTranslationScale.ToArray(); }

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
        if (particles.Count < 3) return;

        renderMat.SetVectorArray("_Particle", particles.ToArray());
        renderMat.SetInt("_ParticleCount", particles.Count);
        renderMat.SetFloat("_UnionSmoothness", unionSmoothness);
        renderMat.SetMatrix("_4x4Identity", Matrix4x4.identity);

        SetPerParticleBridgeData();
    }

    private void SetPerParticleBridgeData()
    {
        _particleBridgeRotMatrix = new List<Matrix4x4>();
        _particleConnectionTranslationScale = new List<Vector4>();
        // need to skip over the first particle
        for (int i = 1; i < particles.Count; i++)
        {
            CalcParticleBridgeTransforms(particles.ToArray(), i);
        }

        renderMat.SetMatrixArray("_BridgeRotationMatrix", _particleBridgeRotMatrix.ToArray());
        renderMat.SetVectorArray("_BridgeData", _particleConnectionTranslationScale.ToArray());
        renderMat.SetInt("_BridgeCount", _particleConnectionTranslationScale.Count);
    }

    private void CalcParticleBridgeTransforms(Vector4[] particles, int index)
    {
        Vector4[] allOtherParticles = particles.Where(p => p != particles[index]).ToArray();
        _distances = new List<float>();

        var particleQuery = allOtherParticles.OrderBy(p => Distance(p, particles[index])).ToArray();
        var distQuery = _distances.OrderBy(d => d).ToArray();

        SetBridgeData(particles[index], particleQuery[0], distQuery[0]); // closest other particle
        SetBridgeData(particles[index], particleQuery[1], distQuery[1]); // second closest other particle
    }

    private float Distance(Vector4 p, Vector4 tp)
    {
        float dist = Vector3.Distance(p, tp);
        _distances.Add(dist);
        return dist;
    }

    // if I want to use cylinders where the origin is at the sphere i need to:
    // - Flip the dir
    // - Track the dir in a list and compare them too (if offset and dir exist then return)
    // - pass the Vector3 a coords as the translation 

    private void SetBridgeData(Vector4 particle, Vector4 targetParticle, float dist)
    {
        Vector3 a = new Vector3(particle.x, particle.y, particle.z);
        Vector3 b = new Vector3(targetParticle.x, targetParticle.y, targetParticle.z);
        Vector3 dir = Vector3.Normalize(a - b);

        Quaternion q = Quaternion.FromToRotation(Vector3.up, dir);
        dist /= 2;

        Vector3 offset = a - dir * dist; // use this is we want to have the bridges positioned between the spheres

        foreach (Vector4 p in _particleConnectionTranslationScale)
        {
            Vector3 p3 = new Vector3(p.x, p.y, p.z);
            if (p3 == offset)
            {
                return; // skip this offset if we have already accounted for it
            }
        }

        Matrix4x4 rotMatrix = Matrix4x4.TRS(Vector3.zero, q, Vector3.one);
        Vector4 translateAndScale = new Vector4(offset.x, offset.y, offset.z, dist);


        _particleBridgeRotMatrix.Add(rotMatrix);
        _particleConnectionTranslationScale.Add(translateAndScale);
    }

    public void PrintDebug()
    {
        // foreach (Vector4 v in _particleConnectionTranslationScale) Debug.Log(v);
        foreach (Vector4 v in _particleConnectionTranslationScale) Debug.Log(v);

        // Debug.Log($"_particleConnectionTranslationScale {_particleConnectionTranslationScale.Count}");
        Debug.Log($"vectorQuery {_particleConnectionTranslationScale.Count}");
    }
}