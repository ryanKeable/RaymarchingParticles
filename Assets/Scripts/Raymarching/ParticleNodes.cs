using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
// using Editor.c

[System.Serializable]
public class ParticleNode
{
    public int id;

    public ParticleSystem.Particle particle;

    public Vector3 pos;
    public float scale;
    public float lifetime;
    private List<float> distances;
    private ParticleSystem system;
    private float distThreshold;
    const int maxConnections = 2;

    public ParticleNode(int _id, ParticleSystem.Particle _particle, ParticleSystem _system, float _distThreshold)
    {
        id = _id;
        particle = _particle;
        distThreshold = _distThreshold;
        system = _system;
    }

    public void UpdateParticleNode(Transform t)
    {
        MDebug.LogBlue($" pos {particle.position}");
        pos = t.InverseTransformPoint(particle.position);
        scale = particle.GetCurrentSize(system) / t.localScale.x;
        lifetime = particle.remainingLifetime;
    }

    public void FindCloseConnections(ParticleNode[] _particleNodes, ParticleNodeConnection[] _particleNodeConnections, out List<ParticleNodeConnection> newConnections)
    {
        newConnections = new List<ParticleNodeConnection>();
        List<ParticleNode> closeParticles = FindCloseParticles(_particleNodes);
        if (closeParticles.Count == 0) return;


        CheckToAddConnections(closeParticles.ToArray(), _particleNodeConnections, out newConnections);
    }

    private List<ParticleNode> FindCloseParticles(ParticleNode[] _particleNodes)
    {
        ParticleNode[] allOtherParticles = _particleNodes.Where(p => p.id != id).ToArray(); // only gather particles who ID is not contained in IDs to ignore
        // gather close particles within the dist threshold --  < dist threshold is clamping the distance >.<

        distances = new List<float>();
        List<ParticleNode> closeParticlesQuery = allOtherParticles.Where(p => CheckDistance(p.particle.position, particle.position)).ToList();
        closeParticlesQuery.OrderBy(p => distances);

        if (closeParticlesQuery.Count > maxConnections) closeParticlesQuery.RemoveRange(maxConnections, closeParticlesQuery.Count - maxConnections);

        return closeParticlesQuery;
    }

    private void CheckToAddConnections(ParticleNode[] _closeNodes, ParticleNodeConnection[] _connections, out List<ParticleNodeConnection> _newConnections)
    {
        _newConnections = new List<ParticleNodeConnection>();

        for (int i = 0; i < _closeNodes.Length; i++)
        {
            bool connectionExists = false;
            for (int j = 0; j < _connections.Length; j++)
            {
                if (_connections[j].id == ConnectionId(id, _closeNodes[i].id))
                {
                    connectionExists = true;
                }
            }

            if (!connectionExists)
            {
                _newConnections.Add(AddNewConnection(_closeNodes[i]));
            }
        }
    }

    private ParticleNodeConnection AddNewConnection(ParticleNode _p)
    {
        int connectionId = ConnectionId(id, _p.id);
        return new ParticleNodeConnection(connectionId, this, _p);
    }

    private int ConnectionId(int a, int b)
    {
        return a + b;
    }

    private bool CheckDistance(Vector3 p, Vector3 tp)
    {
        float dist = Vector3.Distance(p, tp);
        distances.Add(dist);
        return dist < distThreshold;
    }
}

[System.Serializable]
public class ParticleNodeConnection
{
    public int id;
    public List<ParticleNode> nodes = new List<ParticleNode>();
    public Matrix4x4 rotMatrix;
    public Vector3 currentScale;
    public Vector3 pos;

    private float length;
    private float growth;
    private float r1; // might end upbeing a Vector2?
    private float r2; // might end upbeing a Vector2?


    // if I want to use cylinders where the origin is at the sphere i need to:
    // - Flip the dir
    // - Track the dir in a list and compare them too (if offset and dir exist then return)
    // - pass the Vector3 a coords as the translation 
    public ParticleNodeConnection(int _id, ParticleNode _a, ParticleNode _b)
    {
        id = _id;
        nodes.Add(_a);
        nodes.Add(_b);
    }

    public ParticleNodeConnection UpdateParticleNodeConnection(float _growthValue, float _distThreshold, float _minCapScale)
    {


        Vector3 dir = Vector3.Normalize(nodes[1].pos - nodes[0].pos);
        float dist = Vector3.Distance(nodes[0].pos, nodes[1].pos);

        // half the distance so our length is correct
        dist /= 2;
        _distThreshold /= 2;

        Quaternion q = Quaternion.FromToRotation(Vector3.up, dir);

        Vector3 offset = nodes[0].pos - dir * dist; // use this is we want to have the Connections positioned between the spheres

        rotMatrix = Matrix4x4.TRS(Vector3.zero, q, Vector3.one);
        pos = nodes[0].pos; // offset

        float targetR1 = nodes[0].scale * 0.75f;
        float targetR2 = nodes[1].scale * 0.75f;

        if (dist > _distThreshold)
        {
            _growthValue = -_growthValue; // flip when we need to remove
            targetR2 = _minCapScale;
        }
        float targetDist = Mathf.Min(dist, _distThreshold);

        ScaleOverTime(_growthValue, targetDist, targetR1, targetR2);

        if (length <= 0) // when we completely shrink, remove this
            return this;

        return null;
    }

    public void ScaleOverTime(float _growthValue, float _dist, float _r1, float _r2)
    {
        // give us a value that is between 0 and 1
        growth = Mathf.Clamp01(growth);
        growth += _growthValue;

        length = Mathf.SmoothStep(0, _dist, growth);
        r1 = Mathf.SmoothStep(0, _r1, growth);
        r2 = Mathf.SmoothStep(0, _r2, growth);

        currentScale = new Vector3(length, r1, r2);
    }
}