using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

// TODO:
/*
"Property (_ConnectionData) exceeds previous array size (7 vs 6). Cap to previous size. Restart Unity to recreate the arrays.
UnityEngine.Material:SetVectorArray (string,UnityEngine.Vector4[])"

-- used connections are not being ignored correctly
-- lerp disconnected connections back to 0 
-- migthe end up needing to pool particlenodes
*/


[ExecuteInEditMode]
public class RaymarchShapeBuffer : MonoBehaviour
{
    public Material renderMat;
    public Transform sphere;
    public Transform cappedCylinder;

    public Vector3 cylinderScale = new Vector3(1, 1, 1);
    public float sphereScalar = 1f;
    public float cylinderScalar = 1f;
    public float cylinderYoffset = 1f;
    public float unionSmoothness = 0.125f;


    private void Update()
    {
        SetRaymarchBuffer();
    }

    public void SetRaymarchBuffer()
    {
        SetShaderProps();
    }

    void SetShaderProps()
    {
        Vector4 _sphere = new Vector4(sphere.localPosition.x, sphere.localPosition.y, sphere.localPosition.z, Mathf.Max(sphereScalar, -1) * sphere.localScale.x / transform.localScale.x);

        renderMat.SetVector("_Sphere", _sphere);
        renderMat.SetVector("_CylinderPos", cappedCylinder.localPosition);
        renderMat.SetVector("_CylinderScale", cylinderScale / transform.localScale.x);
        renderMat.SetFloat("_CylinderYOffset", cylinderYoffset);
        renderMat.SetFloat("_CylinderScalar", Mathf.Max(cylinderScalar, 0.0001f));
        renderMat.SetFloat("_Smoothness", Mathf.Max(unionSmoothness, 0.01f));

        renderMat.SetMatrix("_4x4Identity", Matrix4x4.identity);
    }

}