using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(RaymarchParticleBuffer))]
public class RaymarchParticleBufferEditor : Editor
{

    RaymarchParticleBuffer theItem;
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
        if (timeSinceStartUp == 0f)
        {
            ResetEditorDeltaTime();
        }

        double editorDeltaTime = (EditorApplication.timeSinceStartup - timeSinceStartUp);
        timeSinceStartUp = EditorApplication.timeSinceStartup;

        theItem.SetRaymarchParticleBuffer((float)editorDeltaTime);
    }

    void ResetEditorDeltaTime()
    {
        timeSinceStartUp = EditorApplication.timeSinceStartup;
    }


    public override void OnInspectorGUI()
    {
        base.DrawDefaultInspector();
    }


    private void OnSceneGUI(SceneView _sceneView)
    {
        Handles.color = Color.yellow;
        // Handles.SphereHandleCap(0, Vector3.zero, Quaternion.identity, .25f, EventType.Repaint); // world centre 


        for (int i = 0; i < theItem.Connections; i++)
        {
            Handles.color = Color.cyan;
            Vector3 sphereCapP = HandlePos(theItem.ConnectionPositions[i]);
            Handles.SphereHandleCap(0, sphereCapP, Quaternion.identity, .25f, EventType.Repaint);
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
