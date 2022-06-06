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
        if (!Application.isPlaying && timeSinceStartUp == 0f)
        {
            ResetEditorDeltaTime();
        }

        double editorDeltaTime = (EditorApplication.timeSinceStartup - timeSinceStartUp);
        timeSinceStartUp = EditorApplication.timeSinceStartup;

        if (!Application.isPlaying) theItem.SetRaymarchParticleBuffer((float)editorDeltaTime);
    }

    void ResetEditorDeltaTime()
    {
        timeSinceStartUp = EditorApplication.timeSinceStartup;
    }

    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Reset"))
        {
            theItem.ResetArrays();
        }

        if (GUILayout.Button("Clear"))
        {
            theItem.Clear();
        }
        base.DrawDefaultInspector();
    }

    private void OnSceneGUI(SceneView _sceneView)
    {

        if (UnityEditor.EditorApplication.isCompiling) theItem.Clear();

        Handles.color = Color.yellow;
        // Handles.SphereHandleCap(0, Vector3.zero, Quaternion.identity, .25f, EventType.Repaint); // world centre 

        for (int i = 0; i < theItem._particleNodes.Count; i++)
        {
            int flip = i % 2;
            Handles.color = i == 0 ? Color.cyan : Color.green;
            Handles.Label(ConvertToLocalPos(theItem._particleNodes[i].particlePosition), theItem._particleNodes[i].id.ToString());
        }
    }

    private Vector3 ConvertToLocalPos(Vector3 _point)
    {
        Transform trans = theItem.transform;
        Vector3 renderP = new Vector3(_point.x * trans.localScale.x, _point.y * trans.localScale.y, _point.z * trans.localScale.z);
        renderP += trans.position;
        return renderP;
    }
}
