using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(RaymarchShapeBuffer))]
public class RaymarchShapeBufferEditor : Editor
{

    RaymarchShapeBuffer theItem;

    private void OnEnable()
    {
        theItem = target as RaymarchShapeBuffer;
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
        if (!Application.isPlaying) theItem.SetRaymarchBuffer();
    }

    public override void OnInspectorGUI()
    {
        string toggleKey = theItem.ConnectionsRenderToggle ? "Off" : "On";
        if (GUILayout.Button($"Toggle Connection Rendering: {toggleKey}"))
        {
            theItem.ToggleConnectionRendering();
        }

        base.DrawDefaultInspector();
    }

    private void OnSceneGUI(SceneView _sceneView)
    {

    }

}
