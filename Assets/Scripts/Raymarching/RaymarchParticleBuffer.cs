using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// TODO:
/*
"Property (_ConnectionData) exceeds previous array size (7 vs 6). Cap to previous size. Restart Unity to recreate the arrays.
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
    private List<Matrix4x4> _particleConnectionRotMatrix;
    private List<float> _distances;

    private ParticleData[] _Particles;
    private int _count;

    public Vector4[] ParticleConnections { get => _particleConnectionTranslationScale.ToArray(); }

    public struct ParticleTransformData
    {
        public Vector3 pos;
        public float scale;
    }

    public struct ParticleData
    {
        public int id;
        public ParticleTransformData transform;
        public ParticleTransformData[] connections;
    }

    private void Update()
    {
        SetRaymarchParticleBuffer();
    }

    private void OnValidate()
    {
        SetRaymarchParticleBuffer();
    }

    // this only renders correctly after a re-compile?
    public void SetRaymarchParticleBuffer()
    {
        if (particles.Count < 3) return;

        SetParticleData();
        SetPerParticleConnectionData();
        SetMaterialProps();
    }

    private void SetMaterialProps()
    {
        renderMat.SetVectorArray("_Particle", SetParticleDataAsVectorArray());
        renderMat.SetInt("_ParticleCount", _Particles.Length);
        renderMat.SetFloat("_UnionSmoothness", unionSmoothness);
        renderMat.SetMatrix("_4x4Identity", Matrix4x4.identity);

        MDebug.LogBlue($"_particleConnectionRotMatrix {_particleConnectionRotMatrix.Count}");
        MDebug.LogBlue($"_particleConnectionTranslationScale {_particleConnectionTranslationScale.Count}");
        renderMat.SetMatrixArray("_ConnectionRotationMatrix", _particleConnectionRotMatrix.ToArray());
        renderMat.SetVectorArray("_ConnectionData", _particleConnectionTranslationScale.ToArray());
        renderMat.SetInt("_ConnectionCount", _particleConnectionTranslationScale.Count);
    }

    private Vector4[] SetParticleDataAsVectorArray()
    {
        Vector4[] pArray = new Vector4[_Particles.Length];
        for (int i = 0; i < pArray.Length; i++)
        {
            pArray[i] = new Vector4(_Particles[i].transform.pos.x, _Particles[i].transform.pos.y, _Particles[i].transform.pos.z, _Particles[i].transform.scale);
        }

        return pArray;
    }

    private void SetParticleData()
    {
        _Particles = new ParticleData[particles.Count];
        for (int i = 0; i < particles.Count; i++)
        {
            ParticleData data = new ParticleData();
            data.transform.pos = new Vector3(particles[i].x, particles[i].y, particles[i].z);
            data.transform.scale = particles[i].w > 0 ? particles[i].w : 0.025f; // define a default if the element is 0
            data.id = i;
            _Particles[i] = data;
        }
    }

    private void SetPerParticleConnectionData()
    {
        _particleConnectionRotMatrix = new List<Matrix4x4>();
        _particleConnectionTranslationScale = new List<Vector4>();
        // need to skip over the first particle
        for (int i = 1; i < _Particles.Length; i++)
        {
            CalcParticleConnectionTransforms(i);
        }
    }

    private void CalcParticleConnectionTransforms(int index)
    {
        ParticleData[] allOtherParticles = _Particles.Where(p => p.id != _Particles[index].id).ToArray();
        _distances = new List<float>();

        var particleQuery = allOtherParticles.OrderBy(p => Distance(p.transform.pos, _Particles[index].transform.pos)).Select(p => p.transform.pos).ToArray();
        var distQuery = _distances.OrderBy(d => d).ToArray();

        SetConnectionData(particles[index], particleQuery[0], distQuery[0]); // closest other particle
        // SetConnectionData(particles[index], particleQuery[1], distQuery[1]); // second closest other particle
    }

    private float Distance(Vector3 p, Vector3 tp)
    {
        float dist = Vector3.Distance(p, tp);
        _distances.Add(dist);
        return dist;
    }

    // if I want to use cylinders where the origin is at the sphere i need to:
    // - Flip the dir
    // - Track the dir in a list and compare them too (if offset and dir exist then return)
    // - pass the Vector3 a coords as the translation 

    private void SetConnectionData(Vector3 a, Vector3 b, float dist)
    {
        Vector3 dir = Vector3.Normalize(a - b);

        Quaternion q = Quaternion.FromToRotation(Vector3.up, dir);
        dist /= 2;

        Vector3 offset = a - dir * dist; // use this is we want to have the Connections positioned between the spheres

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


        _particleConnectionRotMatrix.Add(rotMatrix);
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