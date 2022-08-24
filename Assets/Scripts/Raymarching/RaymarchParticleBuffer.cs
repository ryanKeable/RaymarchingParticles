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
    bool toggleConnectionsRender = true;


    public List<ParticleNode> particleNodes = new List<ParticleNode>();
    private List<ParticleNode> _nodesToRemove = new List<ParticleNode>();

    public List<ParticleConnection> particleConnections = new List<ParticleConnection>();
    private List<ParticleConnection> _connectionsToRemove = new List<ParticleConnection>();

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

    [SerializeField]
    private Vector4[] _particleConnectionData;

    [SerializeField]
    private Vector4[] _particleConnectionSizeData;

    private Matrix4x4[] _particleConnectionMatrices;



    private const int MaxParticles = 16;

    public int Connections { get => particleConnections.Count; }
    public bool ConnectionsRenderToggle { get => toggleConnectionsRender; }

    private void OnEnable()
    {
        Reset();
    }

    private void LateUpdate()
    {
        float time = Time.deltaTime;
        SetRaymarchParticleBuffer(time);
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

        InitializeParticleData(time);
        GatherNodeData();
        GatherConnectionData();
        SetMaterialData();
    }


    #region  InitalizeParticles

    private void InitializeParticleData(float time)
    {
        float growthValue = time * connectionGrowthValue;
        Utils.SetData(particleSystemToRender, transform, unionSmoothness, distThreshold, growthValue, connectionStretchScale, minConnectionScale);

        GatherParticleSystemData();
    }


    private void GatherParticleSystemData()
    {
        // Set Max Particles of system to render
        if (particleSystemToRender.main.maxParticles != MaxParticles)
        {
            ParticleSystem.MainModule main = particleSystemToRender.main;
            main.maxParticles = MaxParticles;
        }

        // init particle array to max particles
        if (_particles == null || _particles.Length < particleSystemToRender.main.maxParticles)
            _particles = new ParticleSystem.Particle[particleSystemToRender.main.maxParticles];


        // init particle array of active particles
        if (particleSystemToRender.isEmitting)
        {
            _particlesCount = particleSystemToRender.GetParticles(_particles);
            _activeParticles = new ParticleSystem.Particle[_particlesCount]; // this is expensive?
            particleSystemToRender.GetParticles(_activeParticles, _particlesCount);
        }

        if (_particlesCount == 0) Clear();
    }

    #endregion

    #region  Nodes

    private void GatherNodeData()
    {
        AddNewParticleNodes();
        UpdateAndRemoveNodes();
    }

    private void AddNewParticleNodes()
    {
        if (_activeParticles == null || _activeParticles.Length == 0) return;

        // check for new particles
        for (int i = 0; i < _activeParticles.Length; i++)
        {
            bool particleIsTracked = false;
            if (_particleNodesCount > 0)
            {
                foreach (ParticleNode n in particleNodes)
                {
                    if (_activeParticles[i].randomSeed == n.id)
                    {
                        particleIsTracked = true;
                    }
                }
            }

            if (!particleIsTracked)
            {
                ParticleNode newParticle = new ParticleNode(_activeParticles[i]);
                particleNodes.Add(newParticle);
            }
        }

        _particleNodesCount = particleNodes.Count;
    }

    private void UpdateAndRemoveNodes()
    {
        _nodesToRemove.Clear();
        foreach (ParticleNode node in particleNodes)
        {
            node.UpdateParticleNode(_activeParticles, out ParticleNode nodeToRemove);

            if (nodeToRemove != null)
            {
                _nodesToRemove.Add(nodeToRemove);
            }

        }

        particleNodes.RemoveAll(n => _nodesToRemove.Contains(n));
        _particleNodesCount = particleNodes.Count;

        // set the index after shit has been removed??
        // theres gotta be a better way to do this...
        foreach (ParticleNode node in particleNodes)
        {
            node.SetIndex(Array.IndexOf(particleNodes.ToArray(), node));
        }
    }

    #endregion

    #region  Connections

    private void GatherConnectionData()
    {
        if (_particleNodesCount == 0 || _particlesCount == 0) return;
        FindNewConnections();
        UpdateAndRemoveConnections();
    }

    private void FindNewConnections()
    {
        for (int i = 0; i < _particleNodesCount; i++)
        {
            ParticleConnection newConnection = particleNodes[i].FindCloseConnections(particleNodes.ToArray());
            if (newConnection != null) particleConnections.Add(newConnection);
        }
    }
    private void UpdateAndRemoveConnections()
    {
        _connectionsToRemove.Clear();
        foreach (ParticleConnection connection in particleConnections)
        {
            connection.UpdateNodeConnection(out ParticleConnection connectionToRemove);
            if (connectionToRemove != null)
            {
                _connectionsToRemove.Add(connectionToRemove);
            }
        }

        particleConnections.RemoveAll(c => _connectionsToRemove.Contains(c));
        _particleConnectionsCount = particleConnections.Count;
    }
    #endregion

    #region  SetMaterial

    private void SetMaterialData()
    {
        if (_particleNodesCount == 0 || _particlesCount == 0) return;
        SetNodeArrayData();
        SetConnectionArrayData();

        SetParticleMaterialProps();
        SetConnectionMaterialProps();
    }

    private void SetNodeArrayData()
    {

        for (int i = 0; i < _particleNodesCount; i++)
        {
            _particleNodePos[i] = particleNodes[i].ParticleNodePos();
            _particleNodeScalars[i] = particleNodes[i].ParticleNodeScalars();
        }
    }

    private void SetConnectionArrayData()
    {
        for (int j = 0; j < _particleConnectionsCount; j++)
        {
            _particleConnectionData[j] = particleConnections[j].ConnectionData;
            _particleConnectionSizeData[j] = particleConnections[j].GetConnectionSizeData();
            _particleConnectionMatrices[j] = particleConnections[j].RotationMatrix;
        }
    }

    public void ToggleConnectionRendering()
    {
        toggleConnectionsRender = !toggleConnectionsRender;
        if (!toggleConnectionsRender) renderMat.EnableKeyword("_DISABLE_CONNECTIONS");
        else renderMat.DisableKeyword("_DISABLE_CONNECTIONS");
    }

    private void SetParticleMaterialProps()
    {
        renderMat.SetVectorArray("_ParticleNodePos", _particleNodePos);
        renderMat.SetVectorArray("_ParticleNodeScalars", _particleNodeScalars);
        renderMat.SetInt("_ParticleCount", _particleNodesCount);
    }

    private void SetConnectionMaterialProps()
    {
        if (_particleConnectionSizeData.Length <= 0 || _particleConnectionData.Length <= 0 || _particleConnectionMatrices.Length <= 0) return;


        renderMat.SetVectorArray("_ConnectionData", _particleConnectionData);
        renderMat.SetVectorArray("_ConnectionSizeData", _particleConnectionSizeData);
        renderMat.SetMatrixArray("_ConnectionRotationMatrices", _particleConnectionMatrices);
        renderMat.SetFloat("_UnionSmoothness", Mathf.Max(unionSmoothness, 0.001f));
        renderMat.SetMatrix("_4x4Identity", Matrix4x4.identity);
        renderMat.SetInt("_ConnectionCount", _particleConnectionsCount);
    }

    #endregion

    #region  CleanUp

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
        particleNodes.Clear();
        _nodesToRemove.Clear();

        particleConnections.Clear();
        _connectionsToRemove.Clear();
    }

    void ClearArrays()
    {
        if (_particleNodePos != null) Array.Clear(_particleNodePos, 0, _particleNodePos.Length);
        if (_particleNodeScalars != null) Array.Clear(_particleNodeScalars, 0, _particleNodeScalars.Length);
        if (_particleConnectionSizeData != null) Array.Clear(_particleConnectionSizeData, 0, _particleConnectionSizeData.Length);
        if (_particleConnectionData != null) Array.Clear(_particleConnectionData, 0, _particleConnectionData.Length);
        if (_particleConnectionMatrices != null) Array.Clear(_particleConnectionMatrices, 0, _particleConnectionMatrices.Length);
    }

    void ResetLists()
    {
        particleNodes = new List<ParticleNode>();
        _nodesToRemove = new List<ParticleNode>();

        particleConnections = new List<ParticleConnection>();
        _connectionsToRemove = new List<ParticleConnection>();
    }

    void ResetArrays()
    {
        _particleNodePos = new Vector4[MaxParticles];
        _particleNodeScalars = new Vector4[MaxParticles];
        _particleConnectionSizeData = new Vector4[MaxParticles * Utils.MaxConnections];
        _particleConnectionData = new Vector4[MaxParticles * Utils.MaxConnections];
        _particleConnectionMatrices = new Matrix4x4[MaxParticles * Utils.MaxConnections];
    }

    void ResetParticleCounts()
    {
        _particlesCount = 0;
        _particleNodesCount = 0;
        _particleConnectionsCount = 0;
    }

    #endregion

    public void EditorUpdate(float time)
    {
        SetRaymarchParticleBuffer(time);
    }

}

// particles cannot control both ends of the connections AS we need to adjust the smoothness on each end
// connection data should only focus on the half connected to the node
// we need to look at all the connections for each particle