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

    const int maxConnections = 1;

    public static void AllocateResources(ParticleSystem _particeSystem, Transform _localTransform, int maxParticleSize)
    {
        _system = _particeSystem;
        _transform = _localTransform;
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
        return (int)combination;
    }

    private static bool CheckParticleMatch(ParticleSystem.Particle _thisParticle, ParticleSystem.Particle _targetParticle)
    {
        MDebug.LogGreen($"this {Array.IndexOf(_particles, _thisParticle)} target {Array.IndexOf(_particles, _targetParticle)}");
        return Array.IndexOf(_particles, _thisParticle) != Array.IndexOf(_particles, _targetParticle);
    }

    private static bool CheckActiveAndDistance(ParticleSystem.Particle _thisParticle, ParticleSystem.Particle _targetParticle, float _distThrehold)
    {
        float dist = Vector3.Distance(ParticlePostionToWorld(_thisParticle), ParticlePostionToWorld(_targetParticle));
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

    public Vector3 originPos;
    public Matrix4x4 rotMatrix;
    public Vector3 currentScale;

    private float[] particleScales = new float[2];
    public float[] particleLife = new float[2];


    private uint startSeed;
    private uint endSeed;
    private Vector3[] positions = new Vector3[2];
    private Vector3 lastKnownStartPos;
    private Vector3 lastKnownEndPos;

    public float lerpValue;

    private float growth;
    private float r1; // might end upbeing a Vector2?
    private float r2; // might end upbeing a Vector2?

    // if I want to use cylinders where the origin is at the sphere i need to:
    /*
    - the collection of connections is getting cleared to 0
    - the connections IDs are swapping
     - also need to manage the extra scale thats applied to the sdf union when a connection is introduced
    */
    public ParticleNodeConnection(int _id, uint _start, uint _end)
    {
        id = _id;
        startSeed = _start;
        endSeed = _end;
        growth = 0.01f;
    }

    // clean this up
    public ParticleNodeConnection UpdateParticleNodeConnection(ParticleSystem.Particle[] _activeParticles, float _growthValue, float _stretchScale, float _distThreshold, float _minCapScale)
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
            particleScales[0] = _minCapScale;
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
            particleScales[1] = _minCapScale;
            particleLife[1] = 0;
        }

        Vector3 dir;
        // if (particleScales[0] > particleScales[1])
        // {
        //     dir = Vector3.Normalize(positions[1] - positions[0]);
        //     originPos = positions[0];
        // }
        // else
        // {
        //     dir = Vector3.Normalize(positions[0] - positions[1]);
        //     originPos = positions[1];
        //     float flipScale = particleScales[0];
        //     particleScales[0] = particleScales[1];
        //     particleScales[1] = flipScale;
        // }

        dir = Vector3.Normalize(positions[0] - positions[1]);
        Quaternion q = Quaternion.FromToRotation(Vector3.up, dir);
        rotMatrix = Matrix4x4.TRS(Vector3.zero, q, Vector3.one);

        float dist = Vector3.Distance(positions[0], positions[1]);
        // half the distance so our length is correct
        dist /= 2;
        _distThreshold /= 2;

        originPos = positions[0] - dir * dist; // use this is we want to have the Connections positioned between the spheres

        if (dist > _distThreshold)
        {
            MDebug.LogPurple($"{id} dist is beyond our _distThreshold -  flip growth!");

            _growthValue = -_growthValue * 2; // flip when we need to remove
            _distThreshold /= 2; // reduce max length 
        }

        bool particle01Dying = particleLife[0] < 0.5f && particleScales[0] < _minCapScale * 2;
        bool particle02Dying = particleLife[1] < 0.5f && particleScales[1] < _minCapScale * 2;
        if (particle01Dying || particle02Dying)
        {
            MDebug.LogOrange($"{id} particles are dying -  flip growth!");

            _growthValue = -_growthValue * 2; // flip when we need to remove
            _distThreshold /= 2; // reduce max length 
        }

        float targetDist = Mathf.Min(dist, _distThreshold);

        ScaleLengthOnUpdate(_growthValue, targetDist, _stretchScale, _minCapScale);


        if (currentScale.x < 0.001f) // when we completely shrink, remove this
        {
            MDebug.LogGreen($"{id} Length is 0 -  remove me!");
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

    public void ScaleLengthOnUpdate(float _growthValue, float _dist, float _stretchScale, float _minCapScale)
    {
        growth += _growthValue;
        growth = Mathf.Min(_dist, growth);

        lerpValue = growth / _dist; // 0->1 range
        lerpValue = Mathf.Clamp01(lerpValue);
        float length = Mathf.SmoothStep(0, _dist, lerpValue);

        float fullLengthStretch = lerpValue * (1 - _stretchScale); // this can be 0 -- we dont want that we want 1->0.5
        float targetR1 = particleScales[0] * (lerpValue - fullLengthStretch);
        targetR1 = Mathf.Min(_minCapScale, targetR1);
        float targetR2 = particleScales[1] * (lerpValue - fullLengthStretch);
        targetR2 = Mathf.Min(_minCapScale, targetR2);

        currentScale = new Vector3(length, targetR1, targetR2); // getting negative values for scale?!
    }
}