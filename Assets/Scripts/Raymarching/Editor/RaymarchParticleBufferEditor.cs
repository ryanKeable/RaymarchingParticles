using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(RaymarchParticleBuffer))]
public class RaymarchParticleBufferEditor : Editor
{

    static RaymarchParticleBufferEditor bufferEditor;
    RaymarchParticleBuffer theItem;

    double timeSinceStartUp = 0f;

    private void OnEnable()
    {
        // AssignTargets();
        theItem = target as RaymarchParticleBuffer;
        EditorApplication.update += Update;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void AssignTargets()
    {
        if (bufferEditor == null) bufferEditor = new RaymarchParticleBufferEditor();
        if (theItem == null) theItem = target as RaymarchParticleBuffer;
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
            theItem.Reset();
        }

        if (GUILayout.Button("Clear"))
        {
            theItem.Clear();
        }

        string toggleKey = theItem.ConnectionsRenderToggle ? "Off" : "On";
        if (GUILayout.Button($"Toggle Connection Rendering: {toggleKey}"))
        {
            theItem.ToggleConnectionRendering();
        }

        base.DrawDefaultInspector();
    }


    private void OnSceneGUI(SceneView _sceneView)
    {

        if (UnityEditor.EditorApplication.isCompiling) theItem.Clear();

        Handles.color = Color.yellow;
        // Handles.SphereHandleCap(0, Vector3.zero, Quaternion.identity, .25f, EventType.Repaint); // world centre 

        for (int i = 0; i < theItem.particleNodes.Count; i++)
        {
            int flip = i % 2;
            Handles.color = i == 0 ? Color.cyan : Color.green;
            Handles.Label(ConvertToLocalPos(theItem.particleNodes[i].particlePosition), theItem.particleNodes[i].id.ToString());
        }
    }

    private Vector3 ConvertToLocalPos(Vector3 _point)
    {
        Transform trans = theItem.transform;
        Vector3 renderP = new Vector3(_point.x * trans.localScale.x, _point.y * trans.localScale.y, _point.z * trans.localScale.z);
        renderP += trans.position;
        return renderP;
    }


    [UnityEditor.Callbacks.DidReloadScripts]
    private static void StopOnCompile()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += StopOnCompile;
            return;
        }

        EditorApplication.delayCall += StopSystem;
    }

    private static void StopSystem()
    {
        MDebug.LogOrange($"Stop system on Compile");
        // if (bufferEditor == null) bufferEditor.AssignTargets();
        // theItem.Reset();
    }
}
