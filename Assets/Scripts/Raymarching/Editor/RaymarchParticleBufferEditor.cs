using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(RaymarchParticleBuffer))]
public class RaymarchParticleBufferEditor : Editor
{

    RaymarchParticleBuffer theItem;

    float step = 0f;
    float lastStep = 0f;

    static double timeSinceStartUp = 0f;

    private void OnEnable()
    {
        theItem = target as RaymarchParticleBuffer;
        EditorApplication.update += Update;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        EditorApplication.update -= Update;
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    public void Update()
    {
        theItem.SetMaterialProps();
    }

    private void OnSceneGUI(SceneView _sceneView)
    {
        for (int i = 0; i < theItem.particles.Count; i++)
        {
            Handles.color = Color.blue;
            Vector3 sphereCapP = HandlePos(theItem.particles[i]);

            Handles.SphereHandleCap(0, sphereCapP, Quaternion.identity, .25f, EventType.Repaint);

            if (i > 0)
            {
                Handles.color = Color.cyan;
                Vector3 prevSphereCapP = HandlePos(theItem.particles[i - 1]);

                Vector3 dir = Vector3.Normalize(sphereCapP - prevSphereCapP);
                float dist = Vector3.Distance(sphereCapP, prevSphereCapP);

                Vector3 midCap = sphereCapP - dir * (dist / 2);
                Handles.SphereHandleCap(0, midCap, Quaternion.identity, .25f, EventType.Repaint);

                Handles.DrawLine(sphereCapP, sphereCapP - dir * dist);
            }
        }
    }

    private Vector3 HandlePos(Vector4 _point)
    {
        Transform trans = theItem.transform;
        Vector3 renderP = new Vector3(_point.x * trans.localScale.x, _point.y * trans.localScale.y, _point.z * trans.localScale.z);
        renderP += trans.position;
        return renderP;
    }

}
