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


    [SerializeField]
    public List<ParticleNode> _particleNodes = new List<ParticleNode>();

    private ParticleSystem.Particle[] _particles;
    private ParticleSystem.Particle[] _activeParticles;

    private Vector4[] _particleTransform;

    private Vector4[] _particleConnectionPos1 = new Vector4[32];
    private Vector4[] _particleConnectionPos2 = new Vector4[32];
    private Vector4[] _particleConnectionScale = new Vector4[32];
    private Matrix4x4[] _particleConnectionMatrices = new Matrix4x4[32];


    public int _particleNodesCount;
    public int _particleConnectionsCount;
    private int _particlesCount;
    private const int MaxParticles = 16;

    public Vector4[] ConnectionPositions1 { get => _particleConnectionPos1; }
    public Vector4[] ConnectionPositions2 { get => _particleConnectionPos2; }
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
        _particleConnectionPos1 = new Vector4[32];
        _particleConnectionPos2 = new Vector4[32];
        _particleConnectionScale = new Vector4[32];
        _particleConnectionMatrices = new Matrix4x4[32];
        _particleTransform = new Vector4[MaxParticles];

        _particlesCount = 0;
        _particleNodesCount = 0;
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


        SetConnectionMaterialProps();
        SetParticleMaterialProps();
    }

    private void SetParticleMaterialProps()
    {
        renderMat.SetVectorArray("_Particle", _particleTransform);
        renderMat.SetInt("_ParticleCount", _particleNodesCount);
        renderMat.SetFloat("_UnionSmoothness", unionSmoothness);
        renderMat.SetMatrix("_4x4Identity", Matrix4x4.identity);

    }

    private void SetConnectionMaterialProps()
    {
        renderMat.SetMatrixArray("_ConnectionRotationMatrix", _particleConnectionMatrices.ToArray());
        renderMat.SetVectorArray("_ConnectionPos1", _particleConnectionPos1);
        renderMat.SetVectorArray("_ConnectionPos2", _particleConnectionPos2);
        renderMat.SetVectorArray("_ConnectionScale", _particleConnectionScale);

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
        SetConnectionData();
    }

    private void FindNewConnections()
    {
        for (int i = 0; i < _particleNodesCount; i++)
        {
            if (_particleNodes[i].isAlive) _particleNodes[i].FindCloseConnections(_particleNodes.ToArray());
        }
    }

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

            _particleConnectionsCount += node.connectionsCount;
        }

        _particleNodes.RemoveAll(n => nodesToRemove.Contains(n));
        _particleNodesCount = _particleNodes.Count;
    }

    private void UpdateParticleData()
    {
        // for (int i = 0; i < _particlesCount; i++)
        // {
        //     Vector3 position = new Vector3(_particles[i].position.x, _particles[i].position.y, _particles[i].position.z);
        //     position = transform.InverseTransformPoint(position);

        //     var scaleQuery = _particleNodeConnections.Where(c => c.startSeed == _activeParticles[i].randomSeed || c.endSeed == _activeParticles[i].randomSeed).Select(c => c.lerpValue).ToArray();

        //     float connectionScalar = 1;
        //     if (scaleQuery.Length > 0)
        //     {
        //         connectionScalar = 0.11f * Mathf.Min(scaleQuery.Length, 3); // 1 .5. 33
        //         float lerpAgg = 0;
        //         foreach (float lerpValue in scaleQuery)
        //         {
        //             lerpAgg += lerpValue;
        //         }
        //         lerpAgg /= scaleQuery.Length;
        //         connectionScalar *= lerpAgg;
        //         connectionScalar = 1 - connectionScalar;
        //     }

        //     // does this also need to affect our connection scale??
        //     // or threre is something wrong with my math
        //     float scale = _activeParticles[i].GetCurrentSize(particleSystemToRender) / transform.localScale.x; // divide by 2 to convert to a radius
        //     scale *= connectionScalar;
        //     _particleTransform[i] = new Vector4(position.x, position.y, position.z, scale);
        // }
    }


    private void SetNodeIndexes()
    {
        for (int i = 0; i < _particleNodesCount; i++)
        {
            _particleNodes[i].SetIndex(_particleNodes.ToArray());
        }
    }

    private void SetConnectionData()
    {
        int runningIndex = 0;
        for (int i = 0; i < _particleNodes.Count; i++)
        {
            _particleTransform[i] = _particleNodes[i].ParticleNodeTransformData();

            for (int j = 0; j < _particleNodes[i].connectionsCount; j++)
            {
                int targetIndex = _particleNodes[i].connections[j].GetNode.index;
                _particleConnectionPos1[runningIndex] = _particleNodes[i].particlePosition;
                _particleConnectionPos2[runningIndex] = _particleNodes[targetIndex].particlePosition;
                _particleConnectionMatrices[runningIndex] = _particleNodes[i].connections[j].Rot;
                _particleConnectionScale[runningIndex] = _particleNodes[i].connections[j].Scale;
                runningIndex++;
            }

        }
    }

    public void EditorUpdate(float time)
    {
        SetRaymarchParticleBuffer(time);
    }
}