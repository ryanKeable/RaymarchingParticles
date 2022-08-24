using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
// using Editor.c


public static class Utils
{
    private static ParticleSystem _system;
    private static Transform _transform;
    private static List<float> _distances;
    // private static ParticleNode[] _particlesNodes;
    private static float _unionSmoothness;
    private static float _distThreshold;
    private static float _connectionGrowthValue;
    private static float _stretchScale;
    private static float _minScale;

    public static int MaxConnections { get => maxConnections; }
    public static float SmoothnessCap { get => smoothCap; }
    public static float Smoothness { get => _unionSmoothness; }
    public static float ConnectionGrowthValue { get => _connectionGrowthValue; }
    public static float DistThreshold { get => _distThreshold; }
    public static float StretchScale { get => _stretchScale; }
    public static float MinScale { get => _minScale; }

    const int maxConnections = 3;
    const float smoothCap = 0.2f;


    // this seems silly 
    public static void SetData(ParticleSystem _particeSystem, Transform _localTransform, float _smoothness, float _distanceThreshold, float _growthValue, float _fullStretchScale, float _minimumScale)
    {
        if (_system == null) _system = _particeSystem;
        if (_transform == null) _transform = _localTransform;
        _unionSmoothness = _smoothness;
        _connectionGrowthValue = _growthValue;
        _distThreshold = _distanceThreshold;
        _stretchScale = _fullStretchScale;
        _minScale = _minimumScale;
    }

    public static ParticleNode[] FindCloseParticleNodes(ParticleNode[] _activeNodes, ParticleNode _thisNode)
    {

        List<ParticleNode> _allOtherNodes = _activeNodes.ToList();// only gather particles who ID is not contained in IDs to ignore
        _allOtherNodes.Remove(_thisNode);

        // filter existing nodes for nodes that we can potentially make connections to
        var acceptableNodeQuery = _allOtherNodes.Where(n => n.totalConnections < maxConnections).ToArray();
        if (acceptableNodeQuery.Length == 0) return null;

        acceptableNodeQuery = acceptableNodeQuery.Where(n => n.isMature == true).ToArray();
        if (acceptableNodeQuery.Length == 0) return null;

        // gather close particles within the dist threshold --  < dist threshold is clamping the distance >.<
        _distances = new List<float>();
        ParticleNode[] closeParticlesQuery = acceptableNodeQuery.Where(p => CheckActiveAndDistance(p, _thisNode, _distThreshold)).ToArray();
        if (closeParticlesQuery.Length == 0) return null;

        closeParticlesQuery.OrderBy(p => _distances);

        return closeParticlesQuery;
    }

    // we orobably need to re-order these to account for the closest ones first??
    public static ParticleConnection CheckToAddConnections(ParticleNode _thisNode, ParticleNode[] _closeNodes)
    {
        for (int i = 0; i < _closeNodes.Length; i++)
        {
            if (_closeNodes[i].connectionIDs.Count >= maxConnections)
            {
                continue;
            }

            bool connectionExists = _thisNode.connectionIDs.Contains(_closeNodes[i].id);

            if (!connectionExists)
            {
                return AddNewConnection(_thisNode, _closeNodes[i]);
            }
        }

        return null;
    }

    private static ParticleConnection AddNewConnection(ParticleNode _thisNode, ParticleNode _targetNode)
    {
        _thisNode.AddConnection(_targetNode.id);
        _targetNode.AddConnection(_thisNode.id);
        return new ParticleConnection(_thisNode, _targetNode);
    }

    private static bool CheckActiveAndDistance(ParticleNode _thisNode, ParticleNode _targetNode, float _distThrehold)
    {
        float dist = Vector3.Distance(_thisNode.particlePosition, _targetNode.particlePosition);
        _distances.Add(dist);
        return dist < _distThrehold;
    }


    public static uint ConnectionId(uint a, uint b)
    {
        return a + b;
    }

    public static float ClampValueByPercentage(float value)
    {
        return value *= 0.05f;
    }

    public static float CurrentParticleSizeToLocalTransform(ParticleSystem.Particle _particle)
    {
        return LocalTransformScale(Mathf.Min(_particle.GetCurrentSize(_system), _particle.startSize)); ;
    }

    public static float ParticleStartSizeToLocalTransform(ParticleSystem.Particle _particle)
    {
        return LocalTransformScale(_particle.startSize);
    }

    public static float LocalTransformScale(float _value)
    {
        return _value / _transform.localScale.x;
    }

    public static float GetParticleSize(ParticleSystem.Particle _particle)
    {
        return _particle.GetCurrentSize(_system);
    }


    public static float StartParticleSizeToLocalTransform(ParticleSystem.Particle _particle)
    {
        return _particle.startSize / _transform.localScale.x;
    }

    public static Vector3 ParticlePostionToWorld(ParticleSystem.Particle _particle)
    {
        return _transform.InverseTransformPoint(_particle.position);
    }

    // this jumps from 0.01 to 0.1 very

    public static float CappedSmootheness(float scalar)
    {
        // if we are greater or equal to 0.2, set 1 or else be the scalar
        float smoothness = scalar >= smoothCap ? 1 : scalar; // any value over the cap, we want to be one
        smoothness *= _unionSmoothness;


        // new method??
        smoothness = Mathf.Min(_unionSmoothness * scalar, _unionSmoothness);

        return smoothness;
    }


}