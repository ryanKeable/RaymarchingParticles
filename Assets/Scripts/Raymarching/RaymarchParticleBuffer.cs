using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
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
    public float distThreshold = 0.75f;
    public Material renderMat;

    private Particle[] _particles = new Particle[32];
    private Vector4[] _particleConnections = new Vector4[32];
    private Matrix4x4[] _particleConnectionMatrices = new Matrix4x4[32];

    private List<Vector4> _particleConnectionTranslationScale;
    private List<Matrix4x4> _particleConnectionRotMatrix;
    private List<float> _distances;
    private List<int> _indices;


    public Vector4[] ParticleConnections { get => _particleConnectionTranslationScale.ToArray(); }

    public class Particle
    {
        public int id;
        public Vector3 pos;
        public float scale;
        public List<Particle> connections;
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
        renderMat.SetInt("_ParticleCount", _particles.Length);
        renderMat.SetFloat("_UnionSmoothness", unionSmoothness);
        renderMat.SetMatrix("_4x4Identity", Matrix4x4.identity);

        renderMat.SetMatrixArray("_ConnectionRotationMatrix", _particleConnectionMatrices.ToArray());
        renderMat.SetVectorArray("_ConnectionData", _particleConnections.ToArray());
        renderMat.SetInt("_ConnectionCount", _particleConnections.Length);
    }

    private Vector4[] SetParticleDataAsVectorArray()
    {
        Vector4[] pArray = new Vector4[_particles.Length];
        for (int i = 0; i < pArray.Length; i++)
        {
            pArray[i] = new Vector4(_particles[i].pos.x, _particles[i].pos.y, _particles[i].pos.z, _particles[i].scale);
        }

        return pArray;
    }

    private void SetParticleData()
    {
        _particles = new Particle[particles.Count];
        for (int i = 0; i < particles.Count; i++)
        {
            Particle data = new Particle();
            data.pos = new Vector3(particles[i].x, particles[i].y, particles[i].z);
            data.scale = particles[i].w > 0 ? particles[i].w : 0.025f; // define a default if the element is 0
            data.id = i;
            data.connections = new List<Particle>();
            _particles[i] = data;
        }
    }

    private void SetPerParticleConnectionData()
    {
        _particleConnectionRotMatrix = new List<Matrix4x4>();
        _particleConnectionTranslationScale = new List<Vector4>();

        for (int i = 0; i < _particles.Length; i++)
        {
            CalcParticleConnectionTransforms(_particles[i]);
        }

        for (int i = 0; i < _particleConnectionTranslationScale.Count; i++)
        {
            _particleConnections[i] = _particleConnectionTranslationScale[i];
            _particleConnectionMatrices[i] = _particleConnectionRotMatrix[i];

        }
    }

    // there will be some issues with this when we start to ignore points greater than a distance
    // if those connections had existed, they will need to be removed in a different function
    // our indexing will probably break too
    private void CalcParticleConnectionTransforms(Particle particle)
    {
        // all other particles needs to include those we are also already connected to
        // we need to gather all the connecting particle ids and check them against our search p id
        int[] idsToIgnore = (int[])particle.connections.Select(p => p.id).Append(particle.id).ToArray();

        Particle[] allOtherParticles = _particles.Where(p => !idsToIgnore.Contains(p.id)).ToArray(); // only gather particles who ID is not contained in IDs to ignore

        _distances = new List<float>();

        // gather close particles within the dist threshold --  < dist threshold is clamping the distance >.<
        Particle[] closeParticlesQuery = (Particle[])allOtherParticles.OrderBy(p => Distance(p.pos, particle.pos)).ToArray();
        float[] distQuery = (float[])_distances.OrderBy(d => d).ToArray();

        AddParticleToConnections(particle, closeParticlesQuery[0]);
        // if (particle.connections.Count > 0 && !particle.connections.Contains(closeParticlesQuery[0])) particle.connections.Add(closeParticlesQuery[0]); // here we the connecting particle to our particle's connecting list

        int closestParticleIndex = Array.IndexOf(_particles, closeParticlesQuery[0]);
        AddParticleToConnections(_particles[closestParticleIndex], particle);

        int secondClosestParticleIndex = Array.IndexOf(_particles, closeParticlesQuery[1]);
        AddParticleToConnections(_particles[secondClosestParticleIndex], particle);


        // if (distQuery.Length == 0) return; // if no distances are close enough than ignore!
        SetConnectionData(particle.pos, closeParticlesQuery[0].pos, distQuery[0]); // closest other particle
        SetConnectionData(particle.pos, closeParticlesQuery[1].pos, distQuery[1]); // second closest other particle
    }


    private float Distance(Vector3 p, Vector3 tp)
    {
        float dist = Vector3.Distance(p, tp);
        // if (dist > distThreshold) return Mathf.Infinity;
        _distances.Add(dist);
        return dist;
    }

    private void AddParticleToConnections(Particle thisP, Particle pToAdd)
    {
        if (thisP.connections.Count > 0 && !thisP.connections.Contains(pToAdd))
            thisP.connections.Add(pToAdd); // here we the connecting particle to our particle's connecting list

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