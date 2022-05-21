using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.EditorCoroutines.Editor;
using System;
using System.Linq;
// using Editor.c

// [System.Serializable]
public class ParticleNode
{
    public int id;
    public Vector3 pos;
    public float scale;

    private List<ParticleNodeConnection> connections = new List<ParticleNodeConnection>();
    private List<ParticleNodeConnection> existingConnectionsOtherThanMine;
    private RaymarchParticleBuffer buffer;

    const int maxConnections = 3;

    public ParticleNode(RaymarchParticleBuffer _buffer)
    {
        connections = new List<ParticleNodeConnection>();
        buffer = _buffer;
    }

    public void UpdateParticleNode(int _id, Vector3 _pos, float _scale)
    {
        id = _id;
        pos = _pos;
        scale = _scale;
    }

    // there will be some issues with this when we start to ignore points greater than a distance
    // if those connections had existed, they will need to be removed in a different function
    // our indexing will probably break too
    public void CalcParticleConnectionTransforms(ParticleNode[] _particleNodes)
    {
        /*
            - on every update:
            - we want to find the other particles that are the CLOSEST to this one
            - once found, establish a connection
            - if that connection exists via another node, ignore it -- this is next!
            - track that connection
            - if that connection exists and is still the closest, update it
            - if that connection breaks (no longer the closest), remove it 

            - trim close particle query??
            - checking distance against so many other nodes is crazy, also looking through all their connection data for other nodes will get insane 
        */

        ParticleNode[] allOtherParticles = _particleNodes.Where(p => p.id != id).ToArray(); // only gather particles who ID is not contained in IDs to ignore
        SetConnectionsOtherThanMine(allOtherParticles);
        // gather close particles within the dist threshold --  < dist threshold is clamping the distance >.<
        List<ParticleNode> closeParticlesQuery = allOtherParticles.Where(p => Vector3.Distance(p.pos, pos) < buffer.distThreshold).ToList();
        closeParticlesQuery.OrderBy(p => Vector3.Distance(p.pos, pos));


        CheckToRemoveConnections(closeParticlesQuery.ToArray());

        if (closeParticlesQuery.Count == 0) return;
        CheckToAddConnections(closeParticlesQuery.ToArray());
        UpdateConnections();
    }

    private void SetConnectionsOtherThanMine(ParticleNode[] _otherParticleNodes)
    {
        existingConnectionsOtherThanMine = new List<ParticleNodeConnection>();

        foreach (ParticleNode n in _otherParticleNodes)
        {
            if (n.connections.Count > 0) existingConnectionsOtherThanMine.AddRange(n.connections);
        }
    }


    // Look at all our connections
    // for each connection, does it contain the current set of close nodes?
    // is the connection managed by this node?
    // if not, this connection should be severed 
    private void CheckToRemoveConnections(ParticleNode[] _closeNodes)
    {
        if (connections.Count == 0) return;

        foreach (ParticleNodeConnection c in connections)
        {
            for (int i = 0; i < _closeNodes.Length; i++)
            {
                bool hasNode = c.nodes.Contains(_closeNodes[i]); // do we 
                if (!hasNode && !CheckForExistingConnections(_closeNodes[i]))
                {
                    MDebug.LogPink($"Node {id} Remove a connection from {c.nodes[0].id} to {c.nodes[1].id} because it does not contain {_closeNodes[i].id}");
                    RemoveConnection(c); // what if the connections has any other i??
                }
            }

        }
    }

    private void CheckToAddConnections(ParticleNode[] _closeNodes)
    {
        for (int i = 0; i < _closeNodes.Length; i++)
        {
            // do any of our current nodes contain the close node at i?
            bool hasNode = false;
            if (connections.Count > 0)
            {
                foreach (ParticleNodeConnection c in connections)
                {
                    if (c.nodes[0].id == _closeNodes[i].id || c.nodes[1].id == _closeNodes[i].id) hasNode = true;
                }
            }

            if (connections.Count == 0 || !hasNode)
            {
                MDebug.LogPurple($"Node {id} Add a new connection from {id} to {_closeNodes[i].id}");
                if (connections.Count < maxConnections)
                {
                    if (!CheckForExistingConnections(_closeNodes[i])) AddNewConnection(id, _closeNodes[i]); // if connection doesnt exist, add it
                }
            }
        }
    }

    private void UpdateConnections()
    {
        foreach (ParticleNodeConnection c in connections)
        {
            c.UpdateParticleNodeConnection();
        }
    }

    // revisit this so we can correctly identify established connections
    private bool CheckForExistingConnections(ParticleNode _nodeToCheck)
    {
        bool query = false;
        foreach (ParticleNodeConnection c in existingConnectionsOtherThanMine)
        {
            query = c.nodes.Contains(_nodeToCheck) && c.nodes.Contains(this);
        }

        return query;
    }

    private void AddNewConnection(int _id, ParticleNode _p)
    {
        id = _id;
        ParticleNodeConnection newConnection = new ParticleNodeConnection(buffer, this, _p);
        connections.Add(newConnection); // if connection doesnt exist, add it
        // _p.AddOtherConnection(newConnection); // do we need to do this?
        buffer.AddToConnections(newConnection);
    }

    public void AddOtherConnection(ParticleNodeConnection _otherConnection)
    {
        connections.Add(_otherConnection);
    }

    private void RemoveConnection(ParticleNodeConnection brokenConnection)
    {
        connections.Remove(brokenConnection); // if connection doesnt exist, add it
        buffer.RemoveFromConnections(brokenConnection);
        brokenConnection.RemoveConnection();
    }

    // private float Distance(Vector3 p, Vector3 tp)
    // {
    //     float dist = Vector3.Distance(p, tp);
    //     if (dist > buffer.distThreshold) dist = Mathf.Infinity;
    //     distances.Add(dist);
    //     return dist;
    // }
}

[System.Serializable]
public class ParticleNodeConnection
{
    public int id;
    public List<ParticleNode> nodes = new List<ParticleNode>();
    public Matrix4x4 rotMatrix;
    public Vector2 currentScale;
    public Vector3 pos;

    private float length;
    private float scale; // might end upbeing a Vector2?
    private bool scaling;
    private RaymarchParticleBuffer buffer;


    // if I want to use cylinders where the origin is at the sphere i need to:
    // - Flip the dir
    // - Track the dir in a list and compare them too (if offset and dir exist then return)
    // - pass the Vector3 a coords as the translation 
    public ParticleNodeConnection(RaymarchParticleBuffer _buffer, ParticleNode a, ParticleNode b)
    {
        MDebug.LogWhite($"ParticleNodeConnection created from {a.id} to {b.id}");
        id = a.id;
        nodes.Add(a);
        nodes.Add(b);
        buffer = _buffer;

#if !UNITY_EDITOR
        buffer.StartCoroutine(
                        doStandardLerp(buffer.connectionAnimationTime, (float lerp) => { GrowConnection(lerp); }, ()=> {scaling = false}, buffer.connectionAnimationShape)
            );
#endif
    }

    public void UpdateParticleNodeConnection()
    {
        Vector3 dir = Vector3.Normalize(nodes[1].pos - nodes[0].pos);
        float dist = Vector3.Distance(nodes[0].pos, nodes[1].pos);

        Quaternion q = Quaternion.FromToRotation(Vector3.up, dir);
        dist /= 2;

        Vector3 offset = nodes[0].pos - dir * dist; // use this is we want to have the Connections positioned between the spheres

        rotMatrix = Matrix4x4.TRS(Vector3.zero, q, Vector3.one);
        pos = nodes[0].pos; // offset
        length = dist;
        scale = nodes[0].scale / 2;

        if (!scaling) currentScale = new Vector2(scale, length);
    }

    public void RemoveConnection()
    {
        MDebug.LogPurple($"REMOVE CONNECTION");

#if UNITY_EDITOR
        currentScale = new Vector2(0, 0);
#else
        buffer.StartCoroutine(
            doStandardLerp(buffer.connectionAnimationTime, (float lerp) => { ShrinkConnection(lerp); }, RemoveConnectionCompletion, buffer.connectionAnimationShape)
        );
#endif
    }

    public void GrowConnection(float lerp)
    {
        scaling = true;
        float _scale = Mathf.Lerp(0, scale, lerp);
        float _legnth = Mathf.Lerp(0, length, lerp);
        currentScale = new Vector2(_scale, _legnth);
    }

    public void ShrinkConnection(float lerp)
    {
        scaling = true;
        float _scale = Mathf.Lerp(scale, 0, lerp);
        float _legnth = Mathf.Lerp(length, 0, lerp);
        currentScale = new Vector2(_scale, _legnth);
    }

    public void RemoveConnectionCompletion()
    {
        scaling = false;
    }

    static IEnumerator doStandardLerp(float time, Action<float> lerpAction, Action completion, AnimationCurve shape)
    {
        if (time == 0f)
        {
            lerpAction(shape.Evaluate(1.0f));
            if (completion != null) completion();
            yield break;
        }
        float fStartTime = Time.time;
        float fLerpLength = time;
        float fCurrLerp = (Time.time - fStartTime) / fLerpLength;

        while (fCurrLerp <= 1.0f)
        {
            fCurrLerp = (Time.time - fStartTime) / fLerpLength;
            lerpAction(shape.Evaluate(fCurrLerp));
            yield return null;
        }
        if (completion != null) completion();
    }
}