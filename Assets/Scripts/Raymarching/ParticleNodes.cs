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
    public int connectionsCount { get => myConnections.Count; }
    public List<ConnectionData> connections { get => myConnections; }
    // 
    private List<ParticleNode> closeNodes = new List<ParticleNode>();
    private List<ConnectionData> myConnections = new List<ConnectionData>();
    private List<ParticleNode> refConnections = new List<ParticleNode>();

    private float startScale;
    private float stretchScale;
    private float distThreshold;
    private float minScale;
    private float smoothness;

    const float decayPercent = 0.5f;



    [Serializable]
    public class ConnectionData
    {
        public uint nodeID;

        private ParticleNode _node;
        private Matrix4x4 _rotation;
        private Vector3 _targetPos;
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
        public Vector3 TargetPos { get => _targetPos; set => _targetPos = value; }
        public Vector4 Scale { get => _scale; set => _scale = value; }
        public float Growth { get => _growth; set => _growth = value; }
        public float DebugLerpValue { get => _lerpValue; set => _lerpValue = value; }
    }


    // if I want to use cylinders where the origin is at the sphere i need to:
    /*
        -- find connections before determining scale
        -- CRUCIALLY!! IF the particle this node tracks DIES but its REF connections still EXIST, this can no longer manage it?!
        -- we have to transfer owner ship to the other particle nodes
    */
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
    }

    public void AddRefConnection(ParticleNode node)
    {
        refConnections.Add(node);
        connectionIDs.Add(node.id);
    }

    // we need to remove any referenced connections when 
    // A) a particle dies and the connection has retracted
    // B) a connection distance gets too great
    private void RemoveMyConnection(ConnectionData connection)
    {
        myConnections.Remove(connection);
        connection.GetNode.RemoveRefConnection(this);
        connectionIDs.Remove(connection.nodeID);
    }

    public void RemoveRefConnection(ParticleNode node)
    {
        refConnections.Remove(node);
        connectionIDs.Remove(node.id);
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
        isMature = IsMature(theParticle);
        isDecaying = IsDecaying(theParticle);
    }

    public void FindCloseConnections(ParticleNode[] _activeNodes)
    {
        if (!isMature || connectionsCount >= Utils.MaxConnections) return;

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

        UpdateParticleConnections(_growthValue);

        if (CheckForFinishedNodeAfterUpdate()) nodeToRemove = this;

    }

    public Vector4 ParticleNodeTransformData()
    {
        Vector3 position = particlePosition;
        float scale = particleScale;

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

    private bool IsMature(ParticleSystem.Particle? _particle)
    {
        if (_particle == null) return false;
        return ScalePercent() > decayPercent;
    }

    private bool IsDecaying(ParticleSystem.Particle? _particle)
    {
        if (_particle == null) return false;
        return !isMature && particleRemainingLife < 0.5f;
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

    private void UpdateParticleConnections(float _growthValue)
    {
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
    }

    // clean this up
    private void UpdateParticleNodeConnection(ConnectionData connection, float _growthValue, out ConnectionData connectionToRemove)
    {
        Vector3 refPos = connection.GetNode.particlePosition;
        connection.TargetPos = refPos;
        float refScale = connection.GetNode.particleScale;
        float refLife = connection.GetNode.particleRemainingLife;

        bool decayConnection = isDecaying || connection.GetNode.isDecaying;

        Vector3 dir = Vector3.Normalize(particlePosition - refPos);
        Quaternion q = Quaternion.FromToRotation(Vector3.up, dir);
        connection.Rot = Matrix4x4.TRS(Vector3.zero, q, Vector3.one);

        // offset based off smoothness and scale
        // lets fix this so that the scale of our caps neatly matches the surface area of the particle where we intersect it
        // particleTransforms[0] -= dir * (particleScales[0] * 0.66f); 
        // particleTransforms[1] += dir * (particleScales[1] * 0.66f);

        float dist = Vector3.Distance(particlePosition, refPos);


        bool breakConnection = dist > distThreshold; // only do this if we are not already decaying

        float targetDist = Mathf.Min(dist, distThreshold);
        targetDist /= 2; // half the distance so our length is correct

        bool flipGrowth = decayConnection || breakConnection;
        float lerpValue = LerpValue(connection, _growthValue, targetDist, flipGrowth);
        // if (decayConnection) lerpValue *= ScalePercent(); // shrink based on particle's current state of decay
        connection.DebugLerpValue = lerpValue;
        float length = LerpLength(dist, lerpValue) / 2;
        Vector3 capScales = LerpConnectionScale(refScale, lerpValue);


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

    private Vector3 LerpConnectionScale(float refScale, float lerpValue)
    {
        float percentageOfParticle = 0.5f; // TODO: work out a better percentage for this based off union smoothness?? (smoothness * 2)??  -- we only want this to effect the final cap scales, not the mid
        float targetR1 = Mathf.SmoothStep(0, particleScale * percentageOfParticle, lerpValue);
        float targetR2 = Mathf.SmoothStep(0, refScale * percentageOfParticle, lerpValue);


        float fullLengthStretch = lerpValue * (1 - stretchScale); // this can be 0 -- we dont want that we want 1->0.5
        float midR = Mathf.Min(targetR1, targetR2) * 0.66f * (lerpValue - fullLengthStretch);

        return new Vector3(targetR1, targetR2, midR);
    }


    private bool CheckForFinishedNodeAfterUpdate()
    {
        return particleRemainingLife == 0 && connectionIDs.Count == 0;
    }
}