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
    public Material renderMat;


    [SerializeField]
    private List<ParticleNodeConnection> _particleNodeConnections = new List<ParticleNodeConnection>();

    private ParticleSystem.Particle[] _particles;
    private ParticleSystem.Particle[] _activeParticles;
    private Vector4[] _particleTransform = new Vector4[8];

    private Vector4[] _particleConnectionPos = new Vector4[32];
    [SerializeField]
    private Vector4[] _particleConnectionScale = new Vector4[32];
    private Matrix4x4[] _particleConnectionMatrices = new Matrix4x4[32];

    private List<Vector4> _particleConnectionTranslationScale;
    private List<Matrix4x4> _particleConnectionRotMatrix;

    private int _particlesCount;
    private const int MaxParticles = 8;

    private void OnEnable()
    {
        Clear();
    }

    private void LateUpdate()
    {
        SetRaymarchParticleBuffer();
    }

    private void OnValidate()
    {
        SetRaymarchParticleBuffer();
    }

    private void Clear()
    {
        _particleNodeConnections = new List<ParticleNodeConnection>();
        _particleTransform = new Vector4[8];
        _particleConnectionPos = new Vector4[32];
        _particleConnectionScale = new Vector4[32];
        _particleConnectionMatrices = new Matrix4x4[32];
        _particlesCount = 0;

    }

    // this only renders correctly after a re-compile?
    public void SetRaymarchParticleBuffer()
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

        UpdateParticleData();
        SetParticleMaterialProps();

        SetParticleConnectionData();
        SetConnectionMaterialProps();
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
        renderMat.SetVectorArray("_ConnectionPos", _particleConnectionPos);
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

        ParticleSDFUtils.AllocateResources(particleSystemToRender, transform, MaxParticles);

    }

    private void UpdateParticleData()
    {
        for (int i = 0; i < _particlesCount; i++)
        {
            Vector3 position = new Vector3(_particles[i].position.x, _particles[i].position.y, _particles[i].position.z);
            position = transform.InverseTransformPoint(position);
            float scale = _activeParticles[i].GetCurrentSize(particleSystemToRender) / transform.localScale.x;
            _particleTransform[i] = new Vector4(position.x, position.y, position.z, scale);
        }
    }

    private void SetParticleConnectionData()
    {
        if (_particlesCount == 0) return;
        FindNewConnections();
        UpdateConnections();
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

    private void UpdateConnections()
    {
        List<ParticleNodeConnection> connectionsToRemove = new List<ParticleNodeConnection>();
        foreach (ParticleNodeConnection exsitingConnection in _particleNodeConnections)
        {
            ParticleNodeConnection connectionToRemove = exsitingConnection.UpdateParticleNodeConnection(_activeParticles, connectionGrowthValue, distThreshold, minConnectionScale);
            if (connectionToRemove != null) connectionsToRemove.Add(connectionToRemove);
        }

        foreach (ParticleNodeConnection connectionToRemove in connectionsToRemove)
        {
            _particleNodeConnections.Remove(connectionToRemove);
        }
    }

    private void SetConnectionData()
    {
        for (int i = 0; i < _particleNodeConnections.Count; i++)
        {
            _particleConnectionPos[i] = _particleNodeConnections[i].positions[0];
            _particleConnectionScale[i] = _particleNodeConnections[i].currentScale;
            _particleConnectionMatrices[i] = _particleNodeConnections[i].rotMatrix;
        }
    }
}