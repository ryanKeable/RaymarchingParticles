using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
// using Editor.c


[Serializable]
public class ParticleNode
{
    public uint id;
    public int index;

    public Vector3 particlePosition;
    public float particleScale;
    public float particleRemainingLife;
    public bool isAlive; // stops us looking for new connections when dead, or being found
    public bool isMature; // stops us looking for new connections when dead, or being found
    public bool isDecaying; // stops us looking for new connections when dead, or being found

    public List<uint> connectionIDs = new List<uint>();
    public List<ConnectionData> connections { get => myConnections; }
    public int myConnectionsCount { get => myConnections.Count; }
    public int totalConnectionsCount { get => connectionIDs.Count; }
    // 
    private List<ParticleNode> closeNodes = new List<ParticleNode>();
    private List<ConnectionData> myConnections = new List<ConnectionData>();
    private List<ParticleNode> refConnections = new List<ParticleNode>();

    private float startScale;
    private float stretchScale;
    private float distThreshold;
    private float minScale;
    private float smoothness;

    float decayPercent = 0.25f;//{ get => connectionScalar * 0.25f; }

    public float connectionScalar;
    public int MyConnectionsCount;

    public Vector4 ConnectionShaderData()
    {
        if (myConnectionsCount == 0) return Vector4.zero;
        int x = connections[0].GetNode.index;
        int y = myConnectionsCount > 1 ? connections[1].GetNode.index : 0;
        int z = myConnectionsCount > 2 ? connections[2].GetNode.index : 0;
        return new Vector4(myConnectionsCount, x, y, z); // this will fail big time
    }

    [Serializable]
    public class ConnectionData
    {
        public uint nodeID;

        private ParticleNode _node;
        private Matrix4x4 _rotation;
        private Vector3 _startPos;
        private Vector3 _endPos;
        private Vector4 _scale;
        private float _growth;
        private float _lerpValue;

        public ConnectionData(ParticleNode node)
        {
            this._node = node;
            this.nodeID = node.id;
            this._lerpValue = 0;
            this._growth = 0;
        }

        public ParticleNode GetNode { get => _node; }
        public Matrix4x4 Rot { get => _rotation; set => _rotation = value; }
        public Vector3 StartPos { get => _startPos; set => _startPos = value; }
        public Vector3 EndPos { get => _endPos; set => _endPos = value; }
        public Vector4 Scale { get => _scale; set => _scale = value; }
        public float Growth { get => _growth; set => _growth = value; }
        public float LerpValue { get => _lerpValue; set => _lerpValue = value; }


    }

    public ParticleNode(ParticleSystem.Particle _particle, float _stretchScale, float _distThreshold, float _minScale, float _smoothness)
    {
        id = _particle.randomSeed;
        stretchScale = _stretchScale;
        distThreshold = _distThreshold / 2;
        minScale = _minScale;
        smoothness = _smoothness;

        myConnections = new List<ConnectionData>();
        refConnections = new List<ParticleNode>();
        connectionIDs = new List<uint>();
    }

    public void AddMyConnection(ParticleNode node)
    {
        myConnections.Add(new ConnectionData(node));
        connectionIDs.Add(node.id);
        MyConnectionsCount++;
    }

    public void AddRefConnection(ParticleNode node)
    {
        refConnections.Add(node);
        connectionIDs.Add(node.id);
    }

    public void RemoveRefConnection(ParticleNode node)
    {
        refConnections.Remove(node);
        connectionIDs.Remove(node.id);
    }

    private void RemoveMyConnection(ConnectionData connection)
    {
        myConnections.Remove(connection);
        connection.GetNode.RemoveRefConnection(this);
        connectionIDs.Remove(connection.nodeID);
        MyConnectionsCount--;
    }

    public void SetParticleData(ParticleSystem.Particle[] _activeParticles)
    {
        ParticleSystem.Particle? theParticle;
        var matchingParticles = _activeParticles.Where(p => p.randomSeed == id).ToArray();
        if (matchingParticles.Length == 0) theParticle = null;
        else theParticle = matchingParticles.FirstOrDefault();

        isAlive = theParticle != null;

        particlePosition = ParticlePosition(theParticle);
        particleScale = ParticleScales(theParticle);
        particleRemainingLife = ParticleRemainingLifetime(theParticle);
        isMature = IsMature();
        isDecaying = IsDecaying();
    }

    public void FindCloseConnections(ParticleNode[] _activeNodes)
    {
        if (!isMature || myConnectionsCount >= Utils.MaxConnections) return;

        closeNodes = Utils.FindCloseParticleNodes(_activeNodes, this, distThreshold);
        if (closeNodes.Count == 0) return;

        Utils.CheckToAddConnections(this, closeNodes.ToArray());
    }

    public void UpdateNode(float _growthValue, out ParticleNode nodeToRemove)
    {
        nodeToRemove = null;

        if (!isAlive)
        {
            CleanUpOnDeath();
            nodeToRemove = this;
            return;
        }

        AdjustScalePerConnection(_growthValue);
    }

    public void UpdateNodeConnections(float _growthValue, out ParticleNode nodeToRemove)
    {
        nodeToRemove = null;
        List<ConnectionData> connectionsToRemove = new List<ConnectionData>();

        for (int i = 0; i < myConnections.Count; i++)
        {
            UpdateParticleNodeConnection(myConnections[i], _growthValue, out ConnectionData connectionToRemove);

            if (connectionToRemove != null) connectionsToRemove.Add(connectionToRemove);
        }

        foreach (ConnectionData c in connectionsToRemove)
        {
            RemoveMyConnection(c);
        }

        if (CheckForFinishedNodeAfterUpdate()) nodeToRemove = this;

    }

    public Vector4 ParticleNodeTransformData()
    {
        Vector3 position = particlePosition;
        float scale = particleScale * Mathf.Pow(connectionScalar, totalConnectionsCount); // do this afterwards??

        return new Vector4(position.x, position.y, position.z, scale);
    }

    public void SetIndex(ParticleNode[] nodes)
    {
        index = Array.IndexOf(nodes, this);
    }

    private Vector3 ParticlePosition(ParticleSystem.Particle? _particle)
    {
        if (_particle == null) return particlePosition;
        return Utils.ParticlePostionToWorld((ParticleSystem.Particle)_particle);
    }

    private float ParticleScales(ParticleSystem.Particle? _particle)
    {
        if (_particle == null) return 0;
        startScale = Utils.ParticleStartSizeToLocalTransform((ParticleSystem.Particle)_particle);
        return Utils.CurrentParticleSizeToLocalTransform((ParticleSystem.Particle)_particle);
    }

    private float ParticleRemainingLifetime(ParticleSystem.Particle? _particle)
    {
        if (_particle == null) return 0;
        ParticleSystem.Particle p = (ParticleSystem.Particle)_particle;
        return p.remainingLifetime;
    }

    private bool IsMature()
    {
        return ScalePercent() > decayPercent;
    }

    private bool IsDecaying()
    {
        return ScalePercent() < 1 - decayPercent && particleRemainingLife < 0.5f;
    }

    private float ScalePercent()
    {
        return particleScale / startScale;
    }

    private float StateOfDecay()
    {
        return particleScale / startScale * decayPercent;
    }

    private void CleanUpOnDeath()
    {
        ConnectionData[] connectionsToRemove = myConnections.ToArray();

        foreach (ConnectionData c in connectionsToRemove)
        {
            RemoveMyConnection(c);
        }
    }

    void AdjustScalePerConnection(float lerpValue)
    {
        // we are trying to account for the growrth that occurs when we union two SDFs in the chader
        if (totalConnectionsCount == 0)
        {
            connectionScalar = 1f;
            return;
        }

        float targetScalar = 1 - smoothness * totalConnectionsCount;
        // float elapsedTime = 1.0f;
        // if (connectionScalar != targetScalar)
        // {
        //     elapsedTime = 0.0f;
        //     elapsedTime += lerpValue;
        // }

        // float scaleTarget = Mathf.Lerp(connectionScalar, targetScalar, elapsedTime);
        connectionScalar = targetScalar;
    }


    // clean this up
    private void UpdateParticleNodeConnection(ConnectionData connection, float _growthValue, out ConnectionData connectionToRemove)
    {
        Vector3 refPos = connection.GetNode.particlePosition;
        connection.EndPos = refPos;
        float refScale = connection.GetNode.particleScale;
        float refConnectionScalar = connection.GetNode.connectionScalar;
        float refLife = connection.GetNode.particleRemainingLife;

        bool decayConnection = isDecaying || connection.GetNode.isDecaying;

        Vector3 dir = Vector3.Normalize(particlePosition - refPos);
        Quaternion q = Quaternion.FromToRotation(Vector3.up, dir);
        connection.Rot = Matrix4x4.TRS(Vector3.zero, q, Vector3.one);

        // offset based off smoothness and scale
        // lets fix this so that the scale of our caps neatly matches the surface area of the particle where we intersect it
        connection.StartPos = particlePosition; // - dir * particleScale * connectionScalar * 0.66f;
        connection.EndPos = refPos; // + dir * refScale * refConnectionScalar * 0.66f;

        float dist = Vector3.Distance(particlePosition, refPos);

        bool breakConnection = dist > distThreshold; // only do this if we are not already decaying

        float targetDist = Mathf.Min(dist, distThreshold);
        targetDist /= 2; // half the distance so our length is correct

        bool flipGrowth = decayConnection || breakConnection;
        float lerpValue = LerpValue(connection, _growthValue, targetDist, flipGrowth);
        // if (decayConnection) lerpValue *= ScalePercent(); // shrink based on particle's current state of decay
        connection.LerpValue = lerpValue;
        float length = LerpLength(dist, lerpValue) / 2;
        Vector3 capScales = LerpConnectionScale(refScale, refConnectionScalar, lerpValue);


        connection.Scale = new Vector4(Mathf.Max(length, 0), capScales.x, capScales.y, capScales.z); // getting negative values for scale?!

        if (flipGrowth && lerpValue < 0.001f) connectionToRemove = connection; // our connection has been completely severed
        else connectionToRemove = null;

    }

    private float LerpValue(ConnectionData connection, float _growthValue, float _dist, bool flip)
    {
        if (flip) _growthValue = -_growthValue;

        connection.Growth += _growthValue;
        connection.Growth = Mathf.Min(_dist, connection.Growth);

        float lerpValue = connection.Growth / _dist; // 0->1 range
        return Mathf.Min(lerpValue, 1);
    }

    private float LerpLength(float _dist, float lerpValue)
    {
        float length = Mathf.SmoothStep(0, _dist, lerpValue);
        length /= 2;

        return length;
    }

    private Vector3 LerpConnectionScale(float refScale, float refConnectionScalar, float lerpValue)
    {
        float percentageOfParticle = .66f;//ScalePercent(); // TODO: work out a better percentage for this based off union smoothness?? (smoothness * 2)??  -- we only want this to effect the final cap scales, not the mid
        float targetR1 = Mathf.SmoothStep(0, particleScale * percentageOfParticle, lerpValue);
        float targetR2 = Mathf.SmoothStep(0, refScale * percentageOfParticle, lerpValue);

        targetR1 *= connectionScalar;
        targetR2 *= refConnectionScalar;

        float fullLengthStretch = lerpValue * (1 - stretchScale); // this can be 0 -- we dont want that we want 1->0.5
        float midR = Mathf.Min(targetR1, targetR2) * 0.8f * (lerpValue - fullLengthStretch);

        // targetR1 = Mathf.Max(targetR1, minScale * startScale);
        // targetR2 = Mathf.Max(targetR2, minScale * startScale);
        // midR = Mathf.Max(midR, minScale * startScale);

        return new Vector3(targetR1, targetR2, midR);
    }


    private bool CheckForFinishedNodeAfterUpdate()
    {
        return particleRemainingLife == 0 && connectionIDs.Count == 0;
    }
}