using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
// using Editor.c

[Serializable]
public class ParticleConnection
{
    public uint targetNodeID;

    private ParticleNode _startNode;
    private ParticleNode _endNode;
    private Matrix4x4 _rotationMatrix;
    private Vector3 _startPos;
    private Vector3 _endPos;
    private Vector4 _connectionData;

    [SerializeField]
    private Vector4 _sizeData;

    private float _dist;
    private float _length;
    private float _startRadius;
    private float _endRadius;
    private float _midRadius;

    [SerializeField]
    private float _growth;

    [SerializeField]
    private float _lerpValue;


    public ParticleConnection(ParticleNode attachedNode, ParticleNode targetNode)
    {
        this._startNode = attachedNode;
        this._endNode = targetNode;
        this.targetNodeID = targetNode.id;
        this._growth = 0;
    }

    public ParticleNode StartNode { get => _startNode; }
    public ParticleNode EndNode { get => _endNode; }
    public Vector4 ConnectionData { get => _connectionData; }
    public Vector4 SizeData { get => _sizeData; }
    public Matrix4x4 RotationMatrix { get => _rotationMatrix; set => _rotationMatrix = value; }

    public void UpdateNodeConnection(out ParticleConnection connectionToRemove)
    {
        connectionToRemove = null;

        // are these still needed??
        // float refScalePercent = EndNode.ScalePercent();
        // float refLife = EndNode.particleRemainingLife;

        CalcAnglesAndDistance(_startNode.particlePosition, _endNode.particlePosition);

        float distThreshold = Utils.DistThreshold / 2; // divide by two to get correct lengths of both cylinder SDFs
        bool breakConnection = _dist > distThreshold;
        bool decayConnection = _startNode.isDying || _endNode.isDying;

        float targetDist = Mathf.Min(_dist, distThreshold);
        targetDist /= 2; // half the distance so our length is correct

        bool flipGrowth = decayConnection || breakConnection;
        SetLerpValue(targetDist, flipGrowth);

        // Set data
        SetLength(_dist);
        SetRadiusScales();
        SetParticleConnectionData();

        // give our nodes their lerpValue
        _startNode.ConnectionScales(_lerpValue);
        _endNode.ConnectionScales(_lerpValue);

        if (flipGrowth && _lerpValue < 0.001f)
        {
            connectionToRemove = this; // our connection has been completely severed
            RemoveConnectionFromNodes();
        }
    }

    private void CalcAnglesAndDistance(Vector3 p1, Vector3 p2)
    {
        Vector3 dir = Vector3.Normalize(p1 - p2);
        Quaternion q = Quaternion.FromToRotation(Vector3.up, dir);
        _rotationMatrix = Matrix4x4.TRS(Vector3.zero, q, Vector3.one);
        _dist = Vector3.Distance(p1, p2);
    }

    private void SetLerpValue(float _dist, bool flip)
    {
        float _growthValue = Utils.ConnectionGrowthValue;

        if (flip) _growthValue = -_growthValue;

        _growth += _growthValue;
        _growth = Mathf.Clamp(_growth, 0, _dist);

        _lerpValue = _growth / _dist; // 0->1 range
    }

    private void SetParticleConnectionData()
    {
        float smoothness = Utils.CappedSmootheness(_lerpValue);
        // float competingConnectionsScalar = StartNode.totalConnectionScalar + EndNode.totalConnectionScalar - _lerpValue;
        // smoothness /= competingConnectionsScalar;
        _connectionData = new Vector4(_startNode.index, _endNode.index, smoothness, 0);
    }

    public Vector4 GetConnectionSizeData()
    {
        return new Vector4(_length, _startRadius, _endRadius, _midRadius);
    }

    private void SetLength(float _dist)
    {
        float length = Mathf.SmoothStep(0, _dist, _lerpValue);
        _length = length * 0.25f;

    }

    private void SetRadiusScales()
    {
        float percentageOfParticle = .8f;

        // min scale stops the pointiness of the connections on growth
        float targetR1 = Mathf.SmoothStep(Utils.MinScale, _startNode.particleScale * percentageOfParticle, _lerpValue);
        float targetR2 = Mathf.SmoothStep(Utils.MinScale, _endNode.particleScale * percentageOfParticle, _lerpValue);

        // targetR1 *= connectionScalar * connectionScalar;
        // targetR2 *= refConnectionScalar * refConnectionScalar;
        float fullLengthStretch = _lerpValue * (1 - Utils.StretchScale); // this can be 0 -- we dont want that we want 1->0.5
        float midR = Mathf.Min(targetR1, targetR2) * 0.5f * (_lerpValue - fullLengthStretch);
        midR = Mathf.SmoothStep(Utils.MinScale, midR, _lerpValue);

        _startRadius = targetR1;
        _endRadius = targetR2;
        _midRadius = midR;
    }

    private void RemoveConnectionFromNodes()
    {
        _startNode.RemoveConnection(_endNode.id);
        _endNode.RemoveConnection(_startNode.id);
    }
}
