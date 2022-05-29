using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

// TODO:
/*
"Property (_ConnectionData) exceeds previous array size (7 vs 6). Cap to previous size. Restart Unity to recreate the arrays.
UnityEngine.Material:SetVectorArray (string,UnityEngine.Vector4[])"

-- used connections are not being ignored correctly
-- lerp disconnected connections back to 0 
*/


[ExecuteInEditMode]
public class RaymarchParticleBuffer : MonoBehaviour
{
    public ParticleSystem particleSystemToRender;
    public float unionSmoothness;
    public float distThreshold = 0.75f;
    public float minConnectionScale = 0.025f;
    public float connectionGrowthValue = 0.25f;
    public float connectionStretchScale = 0.5f;
    public Material renderMat;


    [SerializeField]
    private List<ParticleNodeConnection> _particleNodeConnections = new List<ParticleNodeConnection>();

    private ParticleSystem.Particle[] _particles;
    private ParticleSystem.Particle[] _activeParticles;

    [SerializeField]
    private Vector4[] _particleTransform;

    private Vector4[] _particleConnectionPos1 = new Vector4[32];
    private Vector4[] _particleConnectionPos2 = new Vector4[32];
    private Vector4[] _particleConnectionScale = new Vector4[32];
    private Matrix4x4[] _particleConnectionMatrices = new Matrix4x4[32];



    private int _particlesCount;
    private const int MaxParticles = 16;

    public Vector4[] ConnectionPositions1 { get => _particleConnectionPos1; }
    public Vector4[] ConnectionPositions2 { get => _particleConnectionPos2; }
    public int Connections { get => _particleNodeConnections.Count; }

    private void OnEnable()
    {
        Clear();
    }

    private void LateUpdate()
    {
        float time = Time.deltaTime;
        SetRaymarchParticleBuffer(time);
    }

    private void OnValidate()
    {
        LateUpdate();
    }

    private void Clear()
    {
        _particleNodeConnections = new List<ParticleNodeConnection>();
        _particleConnectionPos1 = new Vector4[32];
        _particleConnectionScale = new Vector4[32];
        _particleConnectionMatrices = new Matrix4x4[32];
        _particleTransform = new Vector4[MaxParticles];

        _particlesCount = 0;

    }

    // this only renders correctly after a re-compile?
    public void SetRaymarchParticleBuffer(float time)
    {
        if (!this.gameObject.activeInHierarchy) return;
        if (particleSystemToRender == null) return;
        if (particleSystemToRender.isStopped)// || !particleSystemToRender.isPlaying)
        {
            Clear();
            return;
        }

        Initialize();
        GatherParticleSystemData();

        SetParticleConnectionData(time);
        SetConnectionMaterialProps();

        UpdateParticleData();
        SetParticleMaterialProps();
    }

    public void AddToConnections(ParticleNodeConnection connection)
    {
        _particleNodeConnections.Add(connection);
    }

    public void RemoveFromConnections(ParticleNodeConnection connection)
    {
        _particleNodeConnections.Remove(connection);
    }

    private void SetParticleMaterialProps()
    {
        renderMat.SetVectorArray("_Particle", _particleTransform);
        renderMat.SetInt("_ParticleCount", _particlesCount);
        renderMat.SetFloat("_UnionSmoothness", unionSmoothness);
        renderMat.SetMatrix("_4x4Identity", Matrix4x4.identity);

    }

    private void SetConnectionMaterialProps()
    {
        renderMat.SetMatrixArray("_ConnectionRotationMatrix", _particleConnectionMatrices.ToArray());
        renderMat.SetVectorArray("_ConnectionPos1", _particleConnectionPos1);
        renderMat.SetVectorArray("_ConnectionPos2", _particleConnectionPos2);
        renderMat.SetVectorArray("_ConnectionScale", _particleConnectionScale);

        renderMat.SetInt("_ConnectionCount", _particleNodeConnections.Count);
    }

    private void GatherParticleSystemData()
    {

        if (particleSystemToRender.isEmitting) _particlesCount = particleSystemToRender.GetParticles(_particles);
        _activeParticles = new ParticleSystem.Particle[_particlesCount];
        particleSystemToRender.GetParticles(_activeParticles, _particlesCount);
    }

    private void Initialize()
    {
        if (particleSystemToRender.main.maxParticles != MaxParticles)
        {
            ParticleSystem.MainModule main = particleSystemToRender.main;
            main.maxParticles = MaxParticles;
        }

        // init particle array
        if (_particles == null || _particles.Length < particleSystemToRender.main.maxParticles)
            _particles = new ParticleSystem.Particle[particleSystemToRender.main.maxParticles];

        if (_particleTransform == null) _particleTransform = new Vector4[MaxParticles];

        ParticleSDFUtils.AllocateResources(particleSystemToRender, transform, MaxParticles);

    }

    private void SetParticleConnectionData(float time)
    {
        if (_particlesCount == 0) return;
        FindNewConnections();
        UpdateConnections(time);
        SetConnectionData();
    }

    private void FindNewConnections()
    {
        for (int i = 0; i < _particlesCount; i++)
        {
            List<ParticleNodeConnection> newConnections = new List<ParticleNodeConnection>();
            ParticleSDFUtils.FindCloseConnections(_activeParticles, _activeParticles[i], _particleNodeConnections.ToArray(), distThreshold, out newConnections);

            if (newConnections.Count > 0)
            {
                _particleNodeConnections.AddRange(newConnections);
            }
        }
    }

    private void UpdateConnections(float time)
    {
        List<ParticleNodeConnection> connectionsToRemove = new List<ParticleNodeConnection>();
        float growthvalue = time * connectionGrowthValue;
        foreach (ParticleNodeConnection exsitingConnection in _particleNodeConnections)
        {
            ParticleNodeConnection connectionToRemove = exsitingConnection.UpdateParticleNodeConnection(_activeParticles, growthvalue, connectionStretchScale, distThreshold, minConnectionScale, unionSmoothness);
            if (connectionToRemove != null) connectionsToRemove.Add(connectionToRemove);
        }

        foreach (ParticleNodeConnection connectionToRemove in connectionsToRemove)
        {
            _particleNodeConnections.Remove(connectionToRemove);
        }
    }

    private void UpdateParticleData()
    {
        for (int i = 0; i < _particlesCount; i++)
        {
            Vector3 position = new Vector3(_particles[i].position.x, _particles[i].position.y, _particles[i].position.z);
            position = transform.InverseTransformPoint(position);

            var scaleQuery = _particleNodeConnections.Where(c => c.startSeed == _activeParticles[i].randomSeed || c.endSeed == _activeParticles[i].randomSeed).Select(c => c.lerpValue).ToArray();

            float connectionScalar = 1;
            if (scaleQuery.Length > 0)
            {
                connectionScalar = 0.11f * Mathf.Min(scaleQuery.Length, 3); // 1 .5. 33
                float lerpAgg = 0;
                foreach (float lerpValue in scaleQuery)
                {
                    lerpAgg += lerpValue;
                }
                lerpAgg /= scaleQuery.Length;
                connectionScalar *= lerpAgg;
                connectionScalar = 1 - connectionScalar;
                MDebug.LogCarnation($"lerpAgg {lerpAgg}");
                MDebug.LogCarnation($"connectionScalar {connectionScalar}");
            }

            // does this also need to affect our connection scale??
            // or threre is something wrong with my math
            float scale = _activeParticles[i].GetCurrentSize(particleSystemToRender) / transform.localScale.x; // divide by 2 to convert to a radius
            scale *= connectionScalar;
            _particleTransform[i] = new Vector4(position.x, position.y, position.z, scale);
        }
    }

    private void SetConnectionData()
    {
        for (int i = 0; i < _particleNodeConnections.Count; i++)
        {
            _particleConnectionPos1[i] = _particleNodeConnections[i].particleTransforms[0];
            _particleConnectionPos2[i] = _particleNodeConnections[i].particleTransforms[1];
            _particleConnectionScale[i] = _particleNodeConnections[i].currentScale;
            _particleConnectionMatrices[i] = _particleNodeConnections[i].rotMatrix;
        }
    }

    public void EditorUpdate(float time)
    {
        SetRaymarchParticleBuffer(time);
    }
}