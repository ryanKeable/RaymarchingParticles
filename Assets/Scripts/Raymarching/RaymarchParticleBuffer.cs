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
    private List<ParticleNode> _nodesToUpdate = new List<ParticleNode>();
    [SerializeField]
    private List<ParticleNodeConnection> _particleNodeConnections = new List<ParticleNodeConnection>();

    private ParticleSystem.Particle[] _particles;
    private ParticleSystem.Particle[] _lastKnownParticles;
    private ParticleNode[] _particleNodes = new ParticleNode[8];
    private Vector4[] _particleTransform = new Vector4[8];
    private Vector4[] _particleConnectionPos = new Vector4[32];
    private Vector4[] _particleConnectionScale = new Vector4[32];
    private Matrix4x4[] _particleConnectionMatrices = new Matrix4x4[32];

    private List<Vector4> _particleConnectionTranslationScale;
    private List<Matrix4x4> _particleConnectionRotMatrix;

    private int _totalParticleCount;
    private int _particlesCount;
    private int _particleNodeCount;
    private const int MaxParticles = 8;

    public Vector3[] ParticleConnections { get => _particleNodeConnections.Select(n => n.pos).ToArray(); }

    private void Start()
    {
        Clear();
    }

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
        _nodesToUpdate = new List<ParticleNode>();
        _particleNodeConnections = new List<ParticleNodeConnection>();
        _particleNodeCount = 0;
        _totalParticleCount = 0;
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

        GatherParticleSystemData();

        ManageParticleNodes();
        UpdateParticleData();
        SetParticleMaterialProps();

        // SetParticleConnectionData();
        // SetConnectionMaterialProps();
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
        renderMat.SetInt("_ParticleCount", _particleNodeCount);
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
        InitParticles();
        if (particleSystemToRender.isEmitting) _particlesCount = particleSystemToRender.GetParticles(_particles);
    }

    private void InitParticles()
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

    private void ManageParticleNodes()
    {
        if (_particleNodeCount == _particlesCount) return;
        if (_nodesToUpdate.Count == _particlesCount) return;

        AddNewParticleNodes();
        // RemoveNodesAndConnections();

        _particleNodeCount = _particlesCount;
        _lastKnownParticles = _particles;
    }

    // this is fucking awful
    private void AddNewParticleNodes()
    {
        List<ParticleNode> nodesToAdd = new List<ParticleNode>();

        // this fucking thing is alwayus maxParticles length zzzzz
        for (int i = 0; i < _particlesCount; i++)
        {
            var nodeIDQuery = _nodesToUpdate.Select(n => n.id).ToArray();
            int id = RandomUtilities.SeededRandomRange(_totalParticleCount, 0, 10000);

            if (nodeIDQuery.Length == 0)
            {
                _totalParticleCount++;
                ParticleNode newNode = new ParticleNode(id, _particles[i], particleSystemToRender, distThreshold);
                _nodesToUpdate.Add(newNode);

                MDebug.LogFern($"Add first new node {newNode.id} _nodesToUpdate.Count {_nodesToUpdate.Count}");

                break;
            }

            bool isTracked = false;
            foreach (int _id in nodeIDQuery)
            {
                if (_id == id) isTracked = true;
            }
            if (!isTracked)
            {
                _totalParticleCount++;
                ParticleNode newNode = new ParticleNode(id, _particles[i], particleSystemToRender, distThreshold);
                _nodesToUpdate.Add(newNode);

                MDebug.LogLtBlue($"{i} Add subsequent new node {newNode.id} _nodesToUpdate.Count {_nodesToUpdate.Count}");
            }
        }

    }

    private void RemoveNodesAndConnections()
    {
        if (_nodesToUpdate.Count <= _particlesCount) return;

        List<ParticleNode> nodesToRemove = new List<ParticleNode>();
        foreach (ParticleSystem.Particle p in _lastKnownParticles)
        {
            foreach (ParticleNode n in _nodesToUpdate)
            {
                if (n.particle.position != p.position) nodesToRemove.Add(n);
            }
        }

        List<ParticleNodeConnection> connectionsToRemove = new List<ParticleNodeConnection>();
        foreach (ParticleNodeConnection existingConnection in _particleNodeConnections)
        {
            bool nodeExists = false;
            foreach (ParticleNode n in nodesToRemove)
            {
                if (existingConnection.nodes[0] != n || existingConnection.nodes[1] != n) nodeExists = true;
            }
            if (!nodeExists) connectionsToRemove.Add(existingConnection);
        }

        foreach (ParticleNodeConnection connectionToRemove in connectionsToRemove)
        {
            _particleNodeConnections.Remove(connectionToRemove);
        }

        foreach (ParticleNode nodeToRemove in nodesToRemove)
        {
            _nodesToUpdate.Remove(nodeToRemove);
            MDebug.LogPink($"Remove node {nodeToRemove.id}");
        }

    }

    private List<ParticleSystem.Particle> FindRemovedParticles()
    {
        List<ParticleSystem.Particle> removedParticles = new List<ParticleSystem.Particle>();

        foreach (ParticleSystem.Particle lp in _lastKnownParticles)
        {
            bool particleExists = false;
            foreach (ParticleSystem.Particle p in _particles)
            {
                if (Array.IndexOf(_lastKnownParticles, lp) == Array.IndexOf(_particles, p)) particleExists = true;
            }

            if (!particleExists) removedParticles.Add(lp);
        }

        return removedParticles;
    }

    private void UpdateParticleData()
    {
        if (_nodesToUpdate.Count == 0) return;

        for (int i = 0; i < _nodesToUpdate.Count; i++)
        {
            _nodesToUpdate[i].UpdateParticleNode(transform);
            _particleTransform[i] = new Vector4(_nodesToUpdate[i].pos.x, _nodesToUpdate[i].pos.y, _nodesToUpdate[i].pos.z, _nodesToUpdate[i].scale);
        }
    }

    private void SetParticleConnectionData()
    {
        if (_nodesToUpdate.Count == 0) return;
        FindNewConnections();
        UpdateConnections();
        SetConnectionData();
    }

    private void FindNewConnections()
    {
        for (int i = 0; i < _nodesToUpdate.Count; i++)
        {
            List<ParticleNodeConnection> newConnections = new List<ParticleNodeConnection>();
            _nodesToUpdate[i].FindCloseConnections(_nodesToUpdate.ToArray(), _particleNodeConnections.ToArray(), out newConnections);

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
            ParticleNodeConnection connectionToRemove = exsitingConnection.UpdateParticleNodeConnection(connectionGrowthValue, distThreshold, minConnectionScale);
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
            _particleConnectionPos[i] = _particleNodeConnections[i].pos;
            _particleConnectionScale[i] = _particleNodeConnections[i].currentScale;
            _particleConnectionMatrices[i] = _particleNodeConnections[i].rotMatrix;
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