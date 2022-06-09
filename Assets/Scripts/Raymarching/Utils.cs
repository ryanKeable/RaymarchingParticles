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
    private static float unionSmoothness;
    public static int MaxConnections { get => maxConnections; }
    public static float SmoothnessCap { get => smoothCap; }
    public static float Smoothness { get => unionSmoothness; }

    const int maxConnections = 3;
    const float smoothCap = 0.2f;

    public static void AllocateResources(ParticleSystem _particeSystem, Transform _localTransform, float _smoothness)
    {
        _system = _particeSystem;
        _transform = _localTransform;
        unionSmoothness = _smoothness;
    }

    // public static void FindCloseConnections(ParticleNode[] _activeNodes, ParticleNode _thisNode, float _distThrehold)
    // {
    //     _particlesNodes = _activeNodes;
    //     List<ParticleNode> closeParticles = FindCloseParticleNodes(_thisNode, _distThrehold);

    //     if (closeParticles.Count == 0) return;


    //     CheckToAddConnections(_thisNode, closeParticles.ToArray());
    // }

    public static List<ParticleNode> FindCloseParticleNodes(ParticleNode[] _activeNodes, ParticleNode _thisNode, float _distThrehold)
    {

        List<ParticleNode> _allOtherNodes = _activeNodes.ToList();// only gather particles who ID is not contained in IDs to ignore
        _allOtherNodes.Remove(_thisNode);

        // filter existing nodes for nodes that we can potentially make connections to
        var acceptableNodeQuery = _allOtherNodes.Where(n => n.myConnectionsCount < maxConnections).ToArray();
        acceptableNodeQuery = acceptableNodeQuery.Where(n => n.isMature == true).ToArray();

        // gather close particles within the dist threshold --  < dist threshold is clamping the distance >.<
        _distances = new List<float>();
        List<ParticleNode> closeParticlesQuery = acceptableNodeQuery.Where(p => CheckActiveAndDistance(p, _thisNode, _distThrehold)).ToList();
        closeParticlesQuery.OrderBy(p => _distances);

        // if (closeParticlesQuery.Count > maxConnections) closeParticlesQuery.RemoveRange(maxConnections, closeParticlesQuery.Count - maxConnections);

        return closeParticlesQuery;
    }

    // we orobably need to re-order these to account for the closest ones first??
    public static void CheckToAddConnections(ParticleNode _thisNode, ParticleNode[] _closeNodes)
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
                AddNewConnection(_thisNode, _closeNodes[i]);
            }
        }
    }

    private static void AddNewConnection(ParticleNode _thisNode, ParticleNode _targetNode)
    {
        _thisNode.AddConnections(_targetNode);
        // _thisNode.AddMyConnection(_targetNode);
        // _targetNode.AddRefConnection(_thisNode);
    }

    private static bool CheckActiveAndDistance(ParticleNode _thisNode, ParticleNode _targetNode, float _distThrehold)
    {
        float dist = Vector3.Distance(_thisNode.particlePosition, _targetNode.particlePosition);
        _distances.Add(dist);
        return dist < _distThrehold;
    }

    public static bool ParticleIsActive(ParticleSystem.Particle _particle)
    {
        if (_particle.randomSeed == 0) return false;
        return CurrentParticleSizeToLocalTransform(_particle) > ClampValueByPercentage(StartParticleSizeToLocalTransform(_particle));
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

    public static float CappedSmootheness(float scalar)
    {
        // if we are greater or equal to 0.2, set 1 or else be the scalar
        float smoothness = scalar >= smoothCap ? 1 : scalar; // any value over the cap, we want to be one
        smoothness *= unionSmoothness;
        return smoothness;
    }


}