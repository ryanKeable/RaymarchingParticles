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
    }

    private void OnDisable()
    {
        EditorApplication.update -= Update;
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

}
