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
-- migthe end up needing to pool particlenodes
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


    public List<ParticleNode> _particleNodes = new List<ParticleNode>();

    private ParticleSystem.Particle[] _particles;
    private ParticleSystem.Particle[] _activeParticles;

    private Vector4[] _particleTransform;

    private Vector4[] _particleConnections = new Vector4[32];
    private float[] _particleConnectionLengths = new float[32];
    private Vector4[] _particleConnectionScale = new Vector4[32];
    private Matrix4x4[] _particleConnectionMatrices = new Matrix4x4[32];


    public int _particleNodesCount;
    public int _particleConnectionsCount;
    private int _particlesCount;
    private const int MaxParticles = 16;

    public int Connections { get => _particleNodes.Count; }

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

    public void Clear()
    {
        _particleNodes = new List<ParticleNode>();

        ClearArrays();

        _particlesCount = 0;
        _particleNodesCount = 0;

        SetParticleMaterialProps();
        SetConnectionMaterialProps();
    }

    void ClearArrays()
    {
        _particleTransform = new Vector4[MaxParticles];
        _particleConnections = new Vector4[MaxParticles * 2];
        _particleConnectionLengths = new float[MaxParticles * 2];
        _particleConnectionScale = new Vector4[MaxParticles * 2];
        _particleConnectionMatrices = new Matrix4x4[MaxParticles * 2];
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


        if (_particleNodes.Count > _particlesCount + 5)
        {
            MDebug.LogRed("DEBUG: Too many nodes, nothing being cleanedup. ABORT");
            return;
        }


        TrackParticleNodes();

        SetNodeParticleData();
        SetParticleConnectionData(time);


        SetParticleMaterialProps();
        SetConnectionMaterialProps();
    }

    private void SetParticleMaterialProps()
    {
        renderMat.SetVectorArray("_Particles", _particleTransform);
        renderMat.SetInt("_ParticleCount", _particleNodesCount);
    }

    private void SetConnectionMaterialProps()
    {

        renderMat.SetVectorArray("_ParticleConnections", _particleConnections);

        renderMat.SetFloatArray("_ConnectionLengths", _particleConnectionLengths);
        renderMat.SetVectorArray("_ConnectionScales", _particleConnectionScale);
        renderMat.SetMatrixArray("_ConnectionRotationMatrices", _particleConnectionMatrices.ToArray());
        renderMat.SetFloat("_UnionSmoothness", Mathf.Max(unionSmoothness, 0.001f));
        renderMat.SetMatrix("_4x4Identity", Matrix4x4.identity);
        renderMat.SetInt("_ConnectionCount", _particleConnectionsCount);
    }

    private void GatherParticleSystemData()
    {
        if (particleSystemToRender.isEmitting) _particlesCount = particleSystemToRender.GetParticles(_particles);
        _activeParticles = new ParticleSystem.Particle[_particlesCount];
        particleSystemToRender.GetParticles(_activeParticles, _particlesCount);

        if (_particlesCount == 0) Clear();
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

        Utils.AllocateResources(particleSystemToRender, transform);

    }

    private void TrackParticleNodes()
    {
        // check for new particles
        for (int i = 0; i < _activeParticles.Length; i++)
        {
            bool particleIsTracked = false;
            foreach (ParticleNode n in _particleNodes)
            {
                if (_activeParticles[i].randomSeed == n.id)
                {
                    particleIsTracked = true;
                }
            }

            if (!particleIsTracked)
            {
                ParticleNode newParticle = new ParticleNode(_activeParticles[i], connectionStretchScale, distThreshold, minConnectionScale, unionSmoothness);
                _particleNodes.Add(newParticle);
            }
        }

        _particleNodesCount = _particleNodes.Count;
    }

    private void SetNodeParticleData()
    {
        for (int i = 0; i < _particleNodesCount; i++)
        {
            _particleNodes[i].SetParticleData(_activeParticles);
        }
    }

    private void SetParticleConnectionData(float time)
    {
        if (_particleNodesCount == 0) return;
        FindNewConnections();
        UpdateNodesAndConnections(time);
        SetNodeIndexes();
        SetShaderArrays();
    }

    private void FindNewConnections()
    {
        for (int i = 0; i < _particleNodesCount; i++)
        {
            if (_particleNodes[i].isAlive) _particleNodes[i].FindCloseConnections(_particleNodes.ToArray());
        }
    }

    // we need to know the current scale of all the particles AFTER the connections have been established
    // this happens here because we lerp the fucker over time??
    private void UpdateNodesAndConnections(float time)
    {
        float growthvalue = time * connectionGrowthValue;
        _particleConnectionsCount = 0;

        List<ParticleNode> nodesToRemove = new List<ParticleNode>();
        foreach (ParticleNode node in _particleNodes)
        {
            node.UpdateNode(growthvalue, out ParticleNode nodeToRemove);
            if (nodeToRemove != null)
            {
                nodesToRemove.Add(nodeToRemove);
            }
        }

        _particleNodes.RemoveAll(n => nodesToRemove.Contains(n));
        nodesToRemove = new List<ParticleNode>(); // reset for connections

        foreach (ParticleNode node in _particleNodes)
        {
            node.UpdateNodeConnections(growthvalue, out ParticleNode nodeToRemove);
            if (nodeToRemove != null)
            {
                nodesToRemove.Add(nodeToRemove);
            }

            _particleConnectionsCount += node.myConnectionsCount;
        }

        _particleNodes.RemoveAll(n => nodesToRemove.Contains(n));
        _particleNodesCount = _particleNodes.Count;
    }

    private void SetNodeIndexes()
    {
        for (int i = 0; i < _particleNodesCount; i++)
        {
            _particleNodes[i].SetIndex(_particleNodes.ToArray());
        }
    }

    // we still need to assign the positions of our target connection as the node may not exist while we still need to access the location
    private void SetShaderArrays()
    {
        ClearArrays();
        int runningIndex = 0;
        for (int i = 0; i < _particleNodes.Count; i++)
        {
            _particleTransform[i] = _particleNodes[i].ParticleNodeTransformData();
            _particleConnections[i] = _particleNodes[i].ConnectionShaderData();

            for (int j = 0; j < _particleNodes[i].MyConnectionsCount; j++)
            {
                _particleConnectionLengths[runningIndex] = _particleNodes[i].connections[j].Length;
                _particleConnectionScale[runningIndex] = _particleNodes[i].connections[j].Scale;
                _particleConnectionMatrices[runningIndex] = _particleNodes[i].connections[j].Rot;
                runningIndex++;
            }

        }


        // var connectionQuery = _particleNodes.SelectMany(n => n.connections).ToArray();
        // for (int j = 0; j < _particleConnectionsCount; j++)
        // {
        //     _particleConnectionPos01[j] = connectionQuery[j].StartPos;
        //     _particleConnectionPos02[j] = connectionQuery[j].EndPos;
        //     _particleConnectionMatrices[j] = connectionQuery[j].Rot;
        //     _particleConnectionScale[j] = connectionQuery[j].Scale;
        // }

    }

    public void EditorUpdate(float time)
    {
        SetRaymarchParticleBuffer(time);
    }
}

// particles cannot control both ends of the connections AS we need to adjust the smoothness on each end
// connection data should only focus on the half connected to the node
// we need to look at all the connections for each particle
// find a way to keep the matrix array the same length (re-use the index) -- maybe a pointer at some point??