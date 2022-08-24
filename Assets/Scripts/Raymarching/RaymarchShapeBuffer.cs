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
    public float cylinderYoffset = 0f;
    public float unionSmoothness = 0.125f;

    private float _cylinderYoffset = 0f;

    bool toggleConnectionsRender = true;
    public bool ConnectionsRenderToggle { get => toggleConnectionsRender; }


    private void Update()
    {
        SetRaymarchBuffer();
    }

    public void SetRaymarchBuffer()
    {
        ShapeProperties();
        SetShaderProps();
    }

    public void ToggleConnectionRendering()
    {
        toggleConnectionsRender = !toggleConnectionsRender;
        if (!toggleConnectionsRender) renderMat.EnableKeyword("_DISABLE_CONNECTIONS");
        else renderMat.DisableKeyword("_DISABLE_CONNECTIONS");
    }

    void ShapeProperties()
    {

        _cylinderYoffset = cylinderYoffset * sphere.localScale.x; // we can go out double the scale so we can scrub back in
        _cylinderYoffset = Mathf.Min(_cylinderYoffset, sphere.localScale.x * 2f); // we can go out double the scale so we can scrub back in
        _cylinderYoffset = Mathf.Max(_cylinderYoffset, 0);

    }

    void SetShaderProps()
    {
        Vector4 _sphere = new Vector4(sphere.localPosition.x, sphere.localPosition.y, sphere.localPosition.z, Mathf.Max(sphereScalar, -1) * sphere.localScale.x / transform.localScale.x);

        renderMat.SetVector("_Sphere", _sphere);
        renderMat.SetVector("_CylinderPos", cappedCylinder.localPosition);
        renderMat.SetVector("_CylinderScale", cylinderScale);
        renderMat.SetFloat("_CylinderYOffset", _cylinderYoffset / transform.localScale.x);
        renderMat.SetFloat("_SphereScalar", Mathf.Max(sphereScalar, 0.0001f));
        renderMat.SetFloat("_CylinderScalar", Mathf.Max(cylinderScalar, 0.0001f));
        renderMat.SetFloat("_Smoothness", Mathf.Max(unionSmoothness, 0.0001f));

        renderMat.SetMatrix("_4x4Identity", Matrix4x4.identity);
    }

}