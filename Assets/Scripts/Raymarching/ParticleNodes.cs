using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
// using Editor.c

//TODO: connections scalar is set to 1 for now
//TODO: myabe have a seperate list of connections again, one PER connection part to draw seperately 
// TODO: essenetially we need a system where each SDF controls its own union smoothness
//TODO: for spheres this is their scale percent
//TODO: for cylinders this is their lerpValue
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
    public List<ConnectionData> MyConnections { get => myConnections; }
    public ConnectionData[] AllConnections { get => myConnections.Concat(refConnections).ToArray(); }
    public int myConnectionsCount { get => myConnections.Count; }
    public int totalConnectionsCount { get => connectionIDs.Count; }
    // 
    private List<ParticleNode> closeNodes = new List<ParticleNode>();

    [SerializeField]
    private List<ConnectionData> myConnections = new List<ConnectionData>();
    [SerializeField]
    private List<ConnectionData> refConnections = new List<ConnectionData>();

    private float startScale;
    private float stretchScale;
    private float distThreshold;
    private float minScale;
    private float smoothness;

    float decayPercent = 0.25f;//{ get => connectionScalar * 0.25f; }

    public float connectionScalar;

    public Vector4 ParticleData()
    {
        return new Vector4(totalConnectionsCount, ScalePercent() * connectionScalar, 0, 0); // this will fail big time
    }

    [Serializable]
    public class ConnectionData
    {
        public uint nodeID;

        private ParticleNode _targetNode;
        private ConnectionData _targetConnection;
        private Matrix4x4 _rotation;
        private Vector3 _startPos;
        private Vector3 _endPos;
        private Vector4 _scale;
        private float _length;
        private float _growth;
        private float _lerpValue;
        private int _rotIndex;


        public ConnectionData(ParticleNode node)
        {
            this._targetNode = node;
            this.nodeID = node.id;
            this._lerpValue = 0;
            this._growth = 0;
        }

        public ParticleNode GetTargetNode { get => _targetNode; }
        public ConnectionData TargetConnection { get => _targetConnection; set => _targetConnection = value; }
        public Matrix4x4 Rot { get => _rotation; set => _rotation = value; }
        public Vector3 StartPos { get => _startPos; set => _startPos = value; }
        public Vector3 EndPos { get => _endPos; set => _endPos = value; }
        public Vector4 Data { get => _scale; set => _scale = value; }
        public float Length { get => _length; set => _length = value; }
        public float Growth { get => _growth; set => _growth = value; }
        public float LerpValue { get => _lerpValue; set => _lerpValue = value; }
        public int RotIndex { get => _rotIndex; set => _rotIndex = value; }


    }

    public ParticleNode(ParticleSystem.Particle _particle, float _stretchScale, float _distThreshold, float _minScale, float _smoothness)
    {
        id = _particle.randomSeed;
        stretchScale = _stretchScale;
        distThreshold = _distThreshold / 2;
        minScale = _minScale;
        smoothness = _smoothness;

        myConnections = new List<ConnectionData>();
        refConnections = new List<ConnectionData>();
        connectionIDs = new List<uint>();
    }

    // we add a connection for ourselves
    // we add a connection for our target
    // the connections point to the node they are connecting to
    // each connection needs to know about their target connection for establishing rotMatrix Ids
    // the target connection is barely used...
    // this all feels convoluted
    public void AddConnections(ParticleNode targetNode)
    {
        ConnectionData newConnection = new ConnectionData(targetNode);
        ConnectionData targetConnection = new ConnectionData(this);
        targetNode.AddRefConnection(targetConnection);
        newConnection.TargetConnection = targetConnection;
        myConnections.Add(newConnection);
        connectionIDs.Add(targetNode.id);
    }

    public void AddRefConnection(ConnectionData connection)
    {
        refConnections.Add(connection);
        connectionIDs.Add(connection.nodeID);
    }

    public void RemoveRefConnection(ConnectionData connection)
    {
        refConnections.Remove(connection);
        connectionIDs.Remove(connection.nodeID);
    }

    private void RemoveMyConnection(ConnectionData connection)
    {
        myConnections.Remove(connection);
        connection.GetTargetNode.RemoveRefConnection(connection.TargetConnection); //remove which connection?? -- it has to match the connection we added
        connectionIDs.Remove(connection.nodeID);
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

    public void UpdateNodeConnections(float _growthValue, ref List<Matrix4x4> rotations, out ParticleNode nodeToRemove)
    {
        nodeToRemove = null;
        List<ConnectionData> connectionsToRemove = new List<ConnectionData>();

        for (int i = 0; i < myConnections.Count; i++)
        {
            TrackNodeConnections(myConnections[i], _growthValue, ref rotations, out ConnectionData connectionToRemove);

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

    public float ScalePercent()
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
        float elapsedTime = 1.0f;
        if (connectionScalar != targetScalar)
        {
            elapsedTime = 0.0f;
            elapsedTime += lerpValue;
        }

        float scaleTarget = Mathf.Lerp(connectionScalar, targetScalar, elapsedTime);
        connectionScalar = scaleTarget;
        connectionScalar = 1f;
    }

    void TrackNodeConnections(ConnectionData connection, float _growthValue, ref List<Matrix4x4> rotations, out ConnectionData connectionToRemove)
    {
        connectionToRemove = null;

        Vector3 refPos = connection.GetTargetNode.particlePosition;
        connection.EndPos = refPos;
        float refScale = connection.GetTargetNode.particleScale;
        float refScalePercent = connection.GetTargetNode.ScalePercent();
        float refConnectionScalar = connection.GetTargetNode.connectionScalar;
        float refLife = connection.GetTargetNode.particleRemainingLife;


        CalcAnglesAndDistance(particlePosition, refPos, out Matrix4x4 rot, out float dist);

        bool breakConnection = dist > distThreshold;
        bool decayConnection = isDecaying || connection.GetTargetNode.isDecaying;

        float targetDist = Mathf.Min(dist, distThreshold);
        targetDist /= 2; // half the distance so our length is correct

        bool flipGrowth = decayConnection || breakConnection;
        float lerpValue = LerpValue(connection, _growthValue, targetDist, flipGrowth);

        connection.LerpValue = lerpValue;


        if (flipGrowth && lerpValue < 0.001f)
        {
            connectionToRemove = connection; // our connection has been completely severed
            return;
        }

        // finish setting data
        // only do this if we are not eing removed
        float length = LerpLength(dist, lerpValue) / 2;
        Vector3 capScales = LerpConnectionScale(refScale, refConnectionScalar, lerpValue);
        rotations.Add(rot);
        int rotMatrixIndex = rotations.Count;
        connection.Data = new Vector4(length, capScales.x, capScales.z, -rotMatrixIndex);  // lerpValue instead of capScales.z
        connection.TargetConnection.Data = new Vector4(length, capScales.y, capScales.z, rotMatrixIndex);  // lerpValue instead of capScales.z

        int rotIndex = Mathf.Abs(rotMatrixIndex);
        int flip = rotMatrixIndex / rotIndex;

        MDebug.LogBlue($"rotIndex {rotIndex} flip {flip}");
    }

    void CalcAnglesAndDistance(Vector3 p1, Vector3 p2, out Matrix4x4 m, out float d)
    {
        Vector3 dir = Vector3.Normalize(p1 - p2);
        Quaternion q = Quaternion.FromToRotation(Vector3.up, dir);
        m = Matrix4x4.TRS(Vector3.zero, q, Vector3.one);
        d = Vector3.Distance(p1, p2);
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

        targetR1 *= connectionScalar * connectionScalar;
        targetR2 *= refConnectionScalar * refConnectionScalar;

        float fullLengthStretch = lerpValue * (1 - stretchScale); // this can be 0 -- we dont want that we want 1->0.5
        float midR = Mathf.Min(targetR1, targetR2) * 0.66f * (lerpValue - fullLengthStretch);

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