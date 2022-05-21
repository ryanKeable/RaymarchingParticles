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
    public List<Vector4> particles = new List<Vector4>();
    public float unionSmoothness;
    public float distThreshold = 0.75f;
    public Material renderMat;

    public AnimationCurve connectionAnimationShape = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float connectionAnimationTime = 0.25f;

    private ParticleNode[] _particleNodes = new ParticleNode[32];
    private List<ParticleNode> _nodesToUpdate = new List<ParticleNode>();

    public List<ParticleNodeConnection> _particleNodeConnections = new List<ParticleNodeConnection>();

    private Vector4[] _particleTransform = new Vector4[32];
    private Vector4[] _particleConnectionPos = new Vector4[32];
    private Vector4[] _particleConnectionScale = new Vector4[32];
    private Matrix4x4[] _particleConnectionMatrices = new Matrix4x4[32];

    private List<Vector4> _particleConnectionTranslationScale;
    private List<Matrix4x4> _particleConnectionRotMatrix;

    private int _particleCount = 0;
    private int _connectionCount;

    public Vector3[] ParticleConnections { get => _particleNodeConnections.Select(n => n.pos).ToArray(); }

    private void Start()
    {
        Clear();
    }


    private void OnEnable()
    {
        Clear();
    }

    private void Update()
    {
        SetRaymarchParticleBuffer();
    }

    private void OnValidate()
    {
        SetRaymarchParticleBuffer();
    }

    private void Clear()
    {
        _nodesToUpdate = new List<ParticleNode>();
        _particleNodeConnections = new List<ParticleNodeConnection>();
        _particleCount = 0;
    }

    // this only renders correctly after a re-compile?
    public void SetRaymarchParticleBuffer()
    {
        if (!this.gameObject.activeInHierarchy) return;
        if (particles.Count < 3) return;

        SetParticles();
        UpdateParticleData();
        SetParticleConnectionData();

        SetParticleMaterialProps();
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
        renderMat.SetInt("_ParticleCount", _particleCount);
        renderMat.SetFloat("_UnionSmoothness", unionSmoothness);
        renderMat.SetMatrix("_4x4Identity", Matrix4x4.identity);

    }

    private void SetConnectionMaterialProps()
    {
        renderMat.SetMatrixArray("_ConnectionRotationMatrix", _particleConnectionMatrices.ToArray());
        renderMat.SetVectorArray("_ConnectionPos", _particleConnectionPos);
        renderMat.SetVectorArray("_ConnectionScale", _particleConnectionScale);

        renderMat.SetInt("_ConnectionCount", _connectionCount);
    }

    private void SetParticles()
    {
        if (_particleCount == particles.Count) return;
        if (_nodesToUpdate.Count == particles.Count) return;

        while (_nodesToUpdate.Count != particles.Count)
        {
            if (_nodesToUpdate.Count < particles.Count)
            {
                _nodesToUpdate.Add(new ParticleNode(this));
                MDebug.LogOrange($"created particle node");
            }
            if (_nodesToUpdate.Count > particles.Count) //Remove a particle at some stgae...
            {
                MDebug.LogOrange($"remove particle node");
            }
        }

        _particleCount = particles.Count;
    }

    private void UpdateParticleData()
    {
        if (_nodesToUpdate.Count == 0) return;

        for (int i = 0; i < _nodesToUpdate.Count; i++)
        {
            Vector3 pos = new Vector3(particles[i].x, particles[i].y, particles[i].z);
            float scale = particles[i].w > 0 ? particles[i].w : 0.025f; // define a default if the element is 0

            _nodesToUpdate[i].UpdateParticleNode(i, pos, scale);

            _particleTransform[i] = new Vector4(pos.x, pos.y, pos.z, scale);
        }
    }

    private void SetParticleConnectionData()
    {
        if (_nodesToUpdate.Count == 0) return;

        for (int i = 0; i < _nodesToUpdate.Count; i++)
        {
            _nodesToUpdate[i].CalcParticleConnectionTransforms(_nodesToUpdate.ToArray());
        }

        _connectionCount = _particleNodeConnections.Count;
        for (int j = 0; j < _particleNodeConnections.Count; j++)
        {
            _particleConnectionPos[j] = _particleNodeConnections[j].pos;
            _particleConnectionScale[j] = _particleNodeConnections[j].currentScale;
            _particleConnectionMatrices[j] = _particleNodeConnections[j].rotMatrix;
        }
    }

    public void PrintDebug()
    {
        // foreach (Vector4 v in _particleConnectionTranslationScale) Debug.Log(v);
        foreach (Vector4 v in _particleConnectionTranslationScale) Debug.Log(v);

        // Debug.Log($"_particleConnectionTranslationScale {_particleConnectionTranslationScale.Count}");
        Debug.Log($"vectorQuery {_particleConnectionTranslationScale.Count}");
    }
}