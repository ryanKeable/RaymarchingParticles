using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

// TODO:
/*
"Property (_ConnectionData) exceeds previous array size (7 vs 6). Cap to previous size. Restart Unity to recreate the arrays.
UnityEngine.Material:SetVectorArray (string,UnityEngine.Vector4[])"

 -- clean up connection storage

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
    private List<Matrix4x4> _particleConnectionRotations = new List<Matrix4x4>();

    private ParticleSystem.Particle[] _particles;
    private ParticleSystem.Particle[] _activeParticles;


    // make sure these are pricvate in the end
    public int _particlesCount;
    public int _particleNodesCount;
    public int _particleConnectionsCount;

    public float debugFirstValue;
    public Vector3 debugRow;
    public Vector3 debugColoumn;

    private Vector4[] _particleNodePos;
    [SerializeField]
    private Vector4[] _particleNodeScalars;
    private Vector4[] _particleConnectionIndexData;
    [SerializeField]
    private Vector4[] _particleConnectionSizeData;
    private Matrix4x4[] _particleConnectionMatrices;



    private const int MaxParticles = 16;

    public int Connections { get => _particleNodes.Count; }

    private void OnEnable()
    {
        Reset();
    }

    private void LateUpdate()
    {
        float time = Time.deltaTime;
        SetRaymarchParticleBuffer(time);
    }

    private void OnValidate()
    {
        Reset();
    }

    public void SetRaymarchParticleBuffer(float time)
    {
        if (!this.gameObject.activeInHierarchy) return;
        if (particleSystemToRender == null) return;
        if (particleSystemToRender.isStopped)// || !particleSystemToRender.isPlaying)
        {
            Clear();
            return;
        }

        InitializeParticleData();
        Utils.AllocateResources(particleSystemToRender, transform, unionSmoothness);
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


    private void InitializeParticleData()
    {
        if (particleSystemToRender.main.maxParticles != MaxParticles)
        {
            ParticleSystem.MainModule main = particleSystemToRender.main;
            main.maxParticles = MaxParticles;
        }

        // init particle array
        if (_particles == null || _particles.Length < particleSystemToRender.main.maxParticles)
            _particles = new ParticleSystem.Particle[particleSystemToRender.main.maxParticles];

    }

    private void GatherParticleSystemData()
    {
        if (particleSystemToRender.isEmitting) _particlesCount = particleSystemToRender.GetParticles(_particles);
        _activeParticles = new ParticleSystem.Particle[_particlesCount]; // this is expensive?
        particleSystemToRender.GetParticles(_activeParticles, _particlesCount);

        if (_particlesCount == 0) Clear();
    }

    public void Reset()
    {
        ResetLists();
        ResetArrays();
        ResetParticleCounts();
        SetParticleMaterialProps();
        SetConnectionMaterialProps();
    }

    public void Clear()
    {
        ClearLists();
        ClearArrays();
        ResetParticleCounts();
        SetParticleMaterialProps();
        SetConnectionMaterialProps();
    }

    void ClearLists()
    {
        _particleNodes.Clear();
        _particleConnectionRotations.Clear();
    }

    void ClearArrays()
    {
        if (_particleNodePos != null) Array.Clear(_particleNodePos, 0, _particleNodePos.Length);
        if (_particleNodeScalars != null) Array.Clear(_particleNodeScalars, 0, _particleNodeScalars.Length);
        if (_particleConnectionSizeData != null) Array.Clear(_particleConnectionSizeData, 0, _particleConnectionSizeData.Length);
        if (_particleConnectionIndexData != null) Array.Clear(_particleConnectionIndexData, 0, _particleConnectionIndexData.Length);
        if (_particleConnectionMatrices != null) Array.Clear(_particleConnectionMatrices, 0, _particleConnectionMatrices.Length);
    }

    void ResetLists()
    {
        _particleNodes = new List<ParticleNode>();
        _particleConnectionRotations = new List<Matrix4x4>();
    }

    void ResetArrays()
    {
        _particleNodePos = new Vector4[MaxParticles];
        _particleNodeScalars = new Vector4[MaxParticles];
        _particleConnectionSizeData = new Vector4[MaxParticles * Utils.MaxConnections];
        _particleConnectionIndexData = new Vector4[MaxParticles * Utils.MaxConnections];
        _particleConnectionMatrices = new Matrix4x4[MaxParticles * Utils.MaxConnections];
    }

    void ResetParticleCounts()
    {
        _particlesCount = 0;
        _particleNodesCount = 0;
        _particleConnectionsCount = 0;
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
                ParticleNode newParticle = new ParticleNode(_activeParticles[i], connectionStretchScale, distThreshold, minConnectionScale);
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
        if (_particleNodesCount == 0 || _particlesCount == 0) return;
        FindNewConnections();
        UpdateNodesAndConnections(time);
        SetNodeArrayData();
        SetConnectionArrayData();
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

        _particleConnectionRotations.Clear();
        float growthvalue = time * connectionGrowthValue;

        // foreach (ParticleNode node in _particleNodes)
        // {
        //     node.UpdateNode(growthvalue, out ParticleNode nodeToRemove);
        //     if (nodeToRemove != null)
        //     {
        //         MDebug.LogGreen($"Remove this node {nodeToRemove.index}");
        //         nodesToRemove.Add(nodeToRemove);
        //     }

        // }
        // _particleNodes.RemoveAll(n => nodesToRemove.Contains(n));

        List<ParticleNode> nodesToRemove = new List<ParticleNode>();
        foreach (ParticleNode node in _particleNodes)
        {
            node.UpdateNodeConnections(growthvalue, ref _particleConnectionRotations, out ParticleNode nodeToRemove);
            if (nodeToRemove != null)
            {
                nodesToRemove.Add(nodeToRemove);
            }
        }

        _particleNodes.RemoveAll(n => nodesToRemove.Contains(n));
        _particleNodesCount = _particleNodes.Count;
    }

    // we still need to assign the positions of our target connection as the node may not exist while we still need to access the location
    private void SetNodeArrayData()
    {

        for (int i = 0; i < _particleNodesCount; i++)
        {
            _particleNodes[i].SetIndex(Array.IndexOf(_particleNodes.ToArray(), _particleNodes[i])); // this needs to happen after we have removed nodes
            _particleNodePos[i] = _particleNodes[i].ParticleNodePos();
            _particleNodeScalars[i] = _particleNodes[i].ParticleNodeScalars();
        }

    }

    private void SetConnectionArrayData()
    {
        // this is a bit sloppy but we can fix it up laer
        var queryTotalConnections = (ParticleConnection[])_particleNodes.SelectMany(n => n.AllConnections).ToArray();
        _particleConnectionsCount = queryTotalConnections.Length;
        for (int j = 0; j < queryTotalConnections.Length; j++)
        {
            _particleConnectionIndexData[j] = queryTotalConnections[j].ParticleConnectionData();
            _particleConnectionSizeData[j] = queryTotalConnections[j].SizeData;
        }

        for (int k = 0; k < _particleConnectionRotations.Count; k++)
        {
            _particleConnectionMatrices[k] = _particleConnectionRotations[k];
        }
    }

    private void SetParticleMaterialProps()
    {
        renderMat.SetVectorArray("_ParticleNodePos", _particleNodePos);
        renderMat.SetVectorArray("_ParticleNodeScalars", _particleNodeScalars);
        renderMat.SetInt("_ParticleCount", _particleNodesCount);
    }

    private void SetConnectionMaterialProps()
    {
        if (_particleConnectionSizeData.Length <= 0 || _particleConnectionIndexData.Length <= 0 || _particleConnectionMatrices.Length <= 0) return;

        renderMat.SetVectorArray("_ConnectionIndexData", _particleConnectionIndexData);
        renderMat.SetVectorArray("_ConnectionSizeData", _particleConnectionSizeData);
        renderMat.SetMatrixArray("_ConnectionRotationMatrices", _particleConnectionMatrices);
        renderMat.SetFloat("_UnionSmoothness", Mathf.Max(unionSmoothness, 0.001f));
        renderMat.SetMatrix("_4x4Identity", Matrix4x4.identity);
        renderMat.SetInt("_ConnectionCount", _particleConnectionsCount);
    }


    public void EditorUpdate(float time)
    {
        SetRaymarchParticleBuffer(time);
    }

}

// particles cannot control both ends of the connections AS we need to adjust the smoothness on each end
// connection data should only focus on the half connected to the node
// we need to look at all the connections for each particle