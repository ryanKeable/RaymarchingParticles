using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
// using Editor.c

public static class ParticleSDFUtils
{
    private static ParticleSystem _system;
    private static Transform _transform;
    private static List<float> _distances;
    private static ParticleSystem.Particle[] _particles;

    const int maxConnections = 2;

    public static void AllocateResources(ParticleSystem _particeSystem, Transform _localTransform, int maxParticleSize)
    {
        _system = _particeSystem;
        _transform = _localTransform;
        // _particles = new ParticleSystem.Particle[maxParticleSize];
    }

    public static void FindCloseConnections(ParticleSystem.Particle[] _particleArray, ParticleSystem.Particle _thisParticle, ParticleNodeConnection[] _particleNodeConnections, float _distThrehold, out List<ParticleNodeConnection> newConnections)
    {
        _particles = _particleArray;
        newConnections = new List<ParticleNodeConnection>();
        List<ParticleSystem.Particle> closeParticles = FindCloseParticles(_thisParticle, _distThrehold);

        if (closeParticles.Count == 0) return;


        CheckToAddConnections(_thisParticle, closeParticles.ToArray(), _particleNodeConnections, out newConnections);
    }

    private static List<ParticleSystem.Particle> FindCloseParticles(ParticleSystem.Particle _thisParticle, float _distThrehold)
    {
        List<ParticleSystem.Particle> _allOtherParticles = _particles.ToList();// only gather particles who ID is not contained in IDs to ignore
        _allOtherParticles.Remove(_thisParticle);

        // gather close particles within the dist threshold --  < dist threshold is clamping the distance >.<
        _distances = new List<float>();
        List<ParticleSystem.Particle> closeParticlesQuery = _allOtherParticles.Where(p => CheckActiveAndDistance(p, _thisParticle, _distThrehold)).ToList();
        closeParticlesQuery.OrderBy(p => _distances);

        if (closeParticlesQuery.Count > maxConnections) closeParticlesQuery.RemoveRange(maxConnections, closeParticlesQuery.Count - maxConnections);

        return closeParticlesQuery;
    }

    private static void CheckToAddConnections(ParticleSystem.Particle _thisParticle, ParticleSystem.Particle[] _closeNodes, ParticleNodeConnection[] _connections, out List<ParticleNodeConnection> _newConnections)
    {
        _newConnections = new List<ParticleNodeConnection>();

        for (int i = 0; i < _closeNodes.Length; i++)
        {
            int id = ConnectionId(_thisParticle, _closeNodes[i]);
            bool connectionExists = _connections.Select(c => c.id).Contains(id);

            if (!connectionExists && ParticleIsActive(_thisParticle) && ParticleIsActive(_closeNodes[i]))
            {
                _newConnections.Add(AddNewConnection(_thisParticle, _closeNodes[i]));
            }
        }
    }

    private static ParticleNodeConnection AddNewConnection(ParticleSystem.Particle _thisParticle, ParticleSystem.Particle _targetParticle)
    {
        int connectionId = ConnectionId(_thisParticle, _targetParticle);
        return new ParticleNodeConnection(connectionId, _thisParticle.randomSeed, _targetParticle.randomSeed);
    }

    private static int ConnectionId(ParticleSystem.Particle _thisParticle, ParticleSystem.Particle _targetParticle)
    {
        uint combination = _thisParticle.randomSeed + _targetParticle.randomSeed;
        return RandomUtilities.SeededRandomRange((int)combination, 0, 10000);
    }

    private static bool CheckParticleMatch(ParticleSystem.Particle _thisParticle, ParticleSystem.Particle _targetParticle)
    {
        MDebug.LogGreen($"this {Array.IndexOf(_particles, _thisParticle)} target {Array.IndexOf(_particles, _targetParticle)}");
        return Array.IndexOf(_particles, _thisParticle) != Array.IndexOf(_particles, _targetParticle);
    }

    private static bool CheckActiveAndDistance(ParticleSystem.Particle _thisParticle, ParticleSystem.Particle _targetParticle, float _distThrehold)
    {
        float dist = Vector3.Distance(_thisParticle.position, _targetParticle.position);
        _distances.Add(dist);
        return dist < _distThrehold;
    }

    public static bool ParticleIsActive(ParticleSystem.Particle _particle)
    {
        if (_particle.randomSeed == 0) return false;
        return CurrentParticleSizeToLocalTransform(_particle) > ClampValueByPercentage(StartParticleSizeToLocalTransform(_particle));
    }

    public static float ClampValueByPercentage(float value)
    {
        return value *= 0.05f;
    }

    public static float CurrentParticleSizeToLocalTransform(ParticleSystem.Particle _particle)
    {
        return Mathf.Min(_particle.GetCurrentSize(_system), _particle.startSize) / _transform.localScale.x;
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
}

[System.Serializable]
public class ParticleNodeConnection
{
    public int id;

    public Matrix4x4 rotMatrix;
    public Vector3[] positions = new Vector3[2];
    public Vector3 currentScale;

    public float[] particleScales = new float[2];
    public float[] particleLife = new float[2];

    private uint startSeed;
    private uint endSeed;
    private Vector3 lastKnownStartPos;
    private Vector3 lastKnownEndPos;


    private float length;
    private float growth;
    private float r1; // might end upbeing a Vector2?
    private float r2; // might end upbeing a Vector2?


    // if I want to use cylinders where the origin is at the sphere i need to:
    /*
     - still need to fix some snapping
     - when a particle dies, we also need to reduce the length of the connection (can't just kill it)
     - also need to manage the extra scale thats applied to the sdf union when a connection is introduced
    */
    public ParticleNodeConnection(int _id, uint _start, uint _end)
    {
        id = _id;
        startSeed = _start;
        endSeed = _end;
    }

    public ParticleNodeConnection UpdateParticleNodeConnection(ParticleSystem.Particle[] _activeParticles, float _growthValue, float _distThreshold, float _minCapScale)
    {
        var startQuery = _activeParticles.Where(p => p.randomSeed == startSeed).ToArray();
        if (startQuery.Length > 0)
        {
            ParticleSystem.Particle startP = startQuery.First();
            positions[0] = ParticleSDFUtils.ParticlePostionToWorld(startP);
            particleScales[0] = ParticleSDFUtils.CurrentParticleSizeToLocalTransform(startP);
            particleLife[0] = startP.remainingLifetime;
        }
        else
        {
            positions[0] = lastKnownStartPos;
            particleScales[0] = 0;
            particleLife[0] = 0;
        }

        var endQuery = _activeParticles.Where(p => p.randomSeed == endSeed).ToArray();
        if (endQuery.Length > 0)
        {
            ParticleSystem.Particle endP = endQuery.First();
            positions[1] = ParticleSDFUtils.ParticlePostionToWorld(endP);
            particleScales[1] = ParticleSDFUtils.CurrentParticleSizeToLocalTransform(endP);
            particleLife[1] = endP.remainingLifetime;
        }
        else
        {
            positions[1] = lastKnownEndPos;
            particleScales[1] = 0;
            particleLife[1] = 0;
        }


        Vector3 dir = Vector3.Normalize(positions[1] - positions[0]);
        float dist = Vector3.Distance(positions[0], positions[1]);

        // half the distance so our length is correct
        dist /= 2;
        _distThreshold /= 2;

        Quaternion q = Quaternion.FromToRotation(Vector3.up, dir);

        // Vector3 offset = nodes[0].pos - dir * dist; // use this is we want to have the Connections positioned between the spheres

        rotMatrix = Matrix4x4.TRS(Vector3.zero, q, Vector3.one);

        float targetR1 = particleScales[0] * 0.5f;
        float targetR2 = particleScales[1] * 0.5f;

        if (dist > _distThreshold)
        {
            _growthValue = -_growthValue; // flip when we need to remove
            targetR2 = _minCapScale;
        }
        float targetDist = Mathf.Min(dist, _distThreshold);

        ScaleOverTime(_growthValue, targetDist, targetR1, targetR2);
        currentScale = new Vector3(length, targetR1, targetR2);

        if (length <= 0.001f) // when we completely shrink, remove this
        {
            return this;
        }

        if (particleLife[0] == 0 && particleLife[1] == 0) // if we run out of time, remove this
        {
            return this;
        }

        lastKnownStartPos = positions[0];
        lastKnownEndPos = positions[1];

        return null;
    }

    public void ScaleOverTime(float _growthValue, float _dist, float _r1, float _r2)
    {
        // give us a value that is between 0 and 1
        growth = Mathf.Clamp01(growth);
        growth += _growthValue;

        length = Mathf.SmoothStep(0, _dist, growth);

    }
}