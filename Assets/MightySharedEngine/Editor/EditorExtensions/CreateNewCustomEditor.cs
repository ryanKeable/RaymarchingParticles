using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

public static class CreateNewCustomEditor
{
    [MenuItem("Assets/Create/C# Custom Editor Script", false, 81), MenuItem("Mighty/Tools/Create Editor Script")]
    private static void CreateCustomEditor()
    {
        UnityEngine.Object obj = Selection.activeObject;
        Type selectedClass = (obj as MonoScript)?.GetClass();
        if (selectedClass == null) {
            Debug.LogError("[CreateNewCustomEditor] Cannot get class from selected object: " + Selection.activeObject?.name);
            return;
        }
        if (!selectedClass.IsSubclassOf(typeof(MonoBehaviour))) {
            Debug.LogError($"[CreateNewCustomEditor] Selected class '{selectedClass}' is not a sub class of MonoBehaviour");
            return;
        }

        string assemblyName = selectedClass.Assembly.GetName().Name;
        string namespaceName = selectedClass.Namespace;
        string className = selectedClass.Name;
        string editorClassName = className + "Editor";

        string dataPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6);
        string assetPath = AssetDatabase.GetAssetPath(obj.GetInstanceID());
        string assetDir = Path.GetDirectoryName(assetPath);

        string editorDir = GetOutputDirForNewEditor(assetDir, assemblyName);
        if (string.IsNullOrEmpty(editorDir)) return;

        string editorScriptPath = $"{editorDir}/{editorClassName}.cs";

        if (File.Exists(editorScriptPath)) {
            MDebug.LogError("[CreateNewCustomEditor] There is already a file here! " + editorScriptPath);
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(editorScriptPath);
            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(editorScriptPath, string.IsNullOrEmpty(namespaceName) ? 13 : 15);
            return;
        }

        if (!Directory.Exists(dataPath + editorDir)) {
            Directory.CreateDirectory(dataPath + editorDir);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        string indent = "";

        List<string> contents = new List<string>(32);
        contents.Add("using UnityEngine;");
        contents.Add("using UnityEditor;");
        contents.Add("");

        if (!string.IsNullOrEmpty(namespaceName)) {
            indent = "    ";
            contents.Add("namespace " + namespaceName);
            contents.Add("{");
        }

        contents.Add(indent + "[CustomEditor(typeof(" + className + "))]");
        contents.Add(indent + "public sealed class " + editorClassName + " : Editor");
        contents.Add(indent + "{");
        contents.Add(indent + "    // void OnEnable()");
        contents.Add(indent + "    // {");
        contents.Add(indent + "    // }");
        contents.Add("");
        contents.Add(indent + "    public override void OnInspectorGUI()");
        contents.Add(indent + "    {");
        contents.Add(indent + "        " + className + " theItem = target as " + className + ";");
        contents.Add("");
        contents.Add(indent + "        if (GUILayout.Button(\"Button One\")) {");
        contents.Add(indent + "            Debug.Log(\"Button One pressed on: \" + theItem);");
        contents.Add(indent + "        }");
        contents.Add("");
        contents.Add(indent + "        EditorGUILayout.Space();");
        contents.Add("");
        contents.Add(indent + "        DrawDefaultInspector();");
        contents.Add(indent + "    }");
        contents.Add("");
        contents.Add(indent + "    // void OnSceneGUI()");
        contents.Add(indent + "    // {");
        contents.Add(indent + "    // }");
        contents.Add(indent + "}");

        if (!string.IsNullOrEmpty(namespaceName)) {
            contents.Add("}");
        }

        File.WriteAllLines(editorScriptPath, contents.ToArray());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(editorScriptPath);
        UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(editorScriptPath, string.IsNullOrEmpty(namespaceName) ? 13 : 15);
    }

    [MenuItem("Assets/Create/C# Custom Editor Script", true, 81)]
    private static bool CreateInspectorValidator()
    {
        return Selection.activeObject is MonoScript;
    }

    private static string GetOutputDirForNewEditor(string originalScriptDir, string assemblyName)
    {
        if (assemblyName == "Assembly-CSharp-Editor-firstpass" || assemblyName == "Assembly-CSharp-Editor" || assemblyName == "Assembly-CSharp-firstpass" || assemblyName == "Assembly-CSharp") {
            // No Assembly Definition file for this script so we can just place it next to the original script
            return originalScriptDir + "/Editor";
        }

        // This script is part of an Assembly Definition file so we need to go up the dir tree to find it
        string searchDir = originalScriptDir;
        while (!string.IsNullOrEmpty(searchDir) && AssetDatabase.LoadAssetAtPath<UnityEngine.Object>($"{searchDir}/{assemblyName}.asmdef") == null) {
            searchDir = Path.GetDirectoryName(searchDir);
        }
        if (string.IsNullOrEmpty(searchDir)) {
            Debug.LogError("[CreateNewCustomEditor] Failed to find the Assembly Definition file that defines the assembly named: " + assemblyName);
            return null;
        }

        return searchDir + "/Editor" + originalScriptDir.Replace(searchDir, "");
    }
}
