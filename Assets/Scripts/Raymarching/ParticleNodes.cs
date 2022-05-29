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

    public Vector3[] particleTransforms = new Vector3[2];
    public float[] particleScales = new float[2];
    public Matrix4x4 rotMatrix;
    public Vector4 currentScale;

    public uint startSeed;
    public uint endSeed;

    private float[] particleLife = new float[2];

    private Vector3 lastKnownStartPos;
    private Vector3 lastKnownEndPos;

    public float lerpValue;

    private float growth;

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
    public ParticleNodeConnection UpdateParticleNodeConnection(ParticleSystem.Particle[] _activeParticles, float _growthValue, float _stretchScale, float _distThreshold, float _minScale, float _smoothness)
    {
        SetParticleData(_activeParticles, startSeed, 0, _minScale, lastKnownStartPos);
        SetParticleData(_activeParticles, endSeed, 1, _minScale, lastKnownEndPos);

        Vector3 dir;
        dir = Vector3.Normalize(particleTransforms[0] - particleTransforms[1]);
        Quaternion q = Quaternion.FromToRotation(Vector3.up, dir);
        rotMatrix = Matrix4x4.TRS(Vector3.zero, q, Vector3.one);

        // offset based off smoothness and scale
        // lets fix this so that the scale of our caps neatly matches the surface area of the particle where we intersect it
        particleTransforms[0] -= dir * (particleScales[0] * 0.66f); // this should position us on the surface of the sphere??
        particleTransforms[1] += dir * (particleScales[1] * 0.66f);

        float dist = Vector3.Distance(particleTransforms[0], particleTransforms[1]);
        // half the distance so our length is correct
        dist /= 2;
        _distThreshold /= 2;


        if (dist > _distThreshold)
        {
            _growthValue = -_growthValue * 2; // flip when we need to remove
            // _distThreshold /= 2; // reduce max length 
        }

        bool particle01Dying = particleLife[0] < 0.5f && particleScales[0] < _minScale * 2;
        bool particle02Dying = particleLife[1] < 0.5f && particleScales[1] < _minScale * 2;
        if (particle01Dying || particle02Dying)
        {
            _growthValue = -_growthValue * 2; // flip when we need to remove
            // _distThreshold /= 2; // reduce max length 
        }

        float targetDist = Mathf.Min(dist, _distThreshold);

        ScaleLengthOnUpdate(_growthValue, targetDist, _stretchScale, _smoothness);


        if (currentScale.x < 0.001f) // when we completely shrink, remove this
        {
            MDebug.LogGreen($"{id} Length is 0 -  remove me!");
            return this;
        }

        if (particleLife[0] == 0 && particleLife[1] == 0) // if we run out of time, remove this
        {
            return this;
        }

        lastKnownStartPos = particleTransforms[0];
        lastKnownEndPos = particleTransforms[1];

        return null;
    }

    void SetParticleData(ParticleSystem.Particle[] _activeParticles, uint seed, int index, float _minScale, Vector3 lastKnownPos)
    {
        var particleQuery = _activeParticles.Where(p => p.randomSeed == seed).ToArray();
        if (particleQuery.Length > 0)
        {
            ParticleSystem.Particle particle = particleQuery.First();
            particleTransforms[index] = ParticleSDFUtils.ParticlePostionToWorld(particle);
            float scale = ParticleSDFUtils.CurrentParticleSizeToLocalTransform(particle);
            particleScales[index] = Mathf.Max(_minScale, scale);
            particleLife[index] = particle.remainingLifetime;
        }
        else
        {
            MDebug.LogBlue($"{id} particle at {seed} no longer exists");
            particleTransforms[index] = lastKnownPos;
            particleScales[index] = _minScale;
            particleLife[index] = 0;
        }
    }

    public void ScaleLengthOnUpdate(float _growthValue, float _dist, float _stretchScale, float _smoothness)
    {
        // account for smoothness offset
        growth += _growthValue;
        growth = Mathf.Min(_dist, growth);

        lerpValue = growth / _dist; // 0->1 range
        lerpValue = Mathf.Clamp01(lerpValue);

        float length = Mathf.SmoothStep(0, _dist, lerpValue);
        length /= 2;

        float percentageOfParticle = 0.5f; // TODO: work out a better percentage for this based off union smoothness?? (smoothness * 2)??  -- we only want this to effect the final cap scales, not the mid
        float targetR1 = particleScales[0] * percentageOfParticle * lerpValue;
        float targetR2 = particleScales[1] * percentageOfParticle * lerpValue;
        // float targetR1 = particleScales[0] * 0.25f * lerpValue;
        // float targetR2 = particleScales[1] * 0.25f * lerpValue;

        float fullLengthStretch = lerpValue * (1 - _stretchScale); // this can be 0 -- we dont want that we want 1->0.5
        float midR = Mathf.Min(targetR1, targetR2) * 0.66f * (lerpValue - fullLengthStretch);

        // targetR2 = Mathf.Max(_minCapScale, targetR2);
        // targetR1 = Mathf.Max(_minCapScale, targetR1);
        // midR = Mathf.Max(_minCapScale, midR);

        currentScale = new Vector4(length, targetR1, targetR2, midR); // getting negative values for scale?!
    }
}