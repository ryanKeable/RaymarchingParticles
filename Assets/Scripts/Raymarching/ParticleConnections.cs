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

    private ParticleNode _attachedNode;
    private ParticleNode _targetNode;
    private ParticleConnection _siblingConnection;
    private Matrix4x4 _rotation;
    private Vector3 _startPos;
    private Vector3 _endPos;
    private Vector4 _sizeData;
    private float _length;
    private float _r1;
    private float _r2;
    private float _growth;
    private int _rotIndex;


    public ParticleConnection(ParticleNode attachedNode, ParticleNode targetNode)
    {
        this._attachedNode = attachedNode;
        this._targetNode = targetNode;
        this.targetNodeID = targetNode.id;
        this._growth = 0;
    }

    public ParticleNode AttachedNode { get => _attachedNode; }
    public ParticleNode TargetNode { get => _targetNode; }
    public ParticleConnection ConnectionSibling { get => _siblingConnection; set => _siblingConnection = value; }
    public Matrix4x4 Rot { get => _rotation; set => _rotation = value; }
    public Vector4 SizeData { get => _sizeData; }
    public float Length { get => _length; set => _length = value; }
    public float Radius01 { get => _r1; set => _r1 = value; }
    public float Radius02 { get => _r2; set => _r2 = value; }
    public float Growth { get => _growth; set => _growth = value; }
    public int RotIndex { get => _rotIndex; set => _rotIndex = value; }
    public int PosIndex { get => _attachedNode.index; }

    public Vector4 ParticleConnectionData()
    {
        return new Vector4(PosIndex, RotIndex, 0, 0);
    }

    public void SetConnectionSizeData(float _length, float _radius1, float _radius2, float _lerpValue)
    {
        float smoothness = Utils.CappedSmootheness(_lerpValue);

        _sizeData = new Vector4(_length, _radius1, _radius2, smoothness);
    }

}
