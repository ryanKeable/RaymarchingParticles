using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

public static class CreateNewBooterScript
{
    [MenuItem("Assets/Create/C# Booter Script", false, 82), MenuItem("Mighty/Tools/Create Booter Script")]
    private static void CreateBooterScript()
    {
        UnityEngine.Object obj = Selection.activeObject;
        Type selectedClass = (obj as MonoScript)?.GetClass();
        if (selectedClass == null) {
            Debug.LogError("[CreateNewBooterScript] Cannot get class from selected object: " + Selection.activeObject?.name);
            return;
        }
        if (!selectedClass.IsSubclassOf(typeof(MonoBehaviour))) {
            Debug.LogError($"[CreateNewBooterScript] Selected class '{selectedClass}' is not a sub class of MonoBehaviour");
            return;
        }

        string assemblyName = selectedClass.Assembly.GetName().Name;
        string namespaceName = selectedClass.Namespace;
        string className = selectedClass.Name;
        string booterClassName = className + "Booter";

        string assetPath = AssetDatabase.GetAssetPath(obj.GetInstanceID());
        string assetDir = Path.GetDirectoryName(assetPath);

        string booterScriptPath = $"{assetDir}/{booterClassName}.cs";

        if (File.Exists(booterScriptPath)) {
            MDebug.LogError("[CreateNewBooterScript] There is already a file here! " + booterScriptPath);
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(booterScriptPath);
            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(booterScriptPath, string.IsNullOrEmpty(namespaceName) ? 8 : 10);
            return;
        }

        string indent = "";

        List<string> contents = new List<string>(32);
        contents.Add("using UnityEngine;");
        contents.Add("using System;");
        contents.Add("");

        if (!string.IsNullOrEmpty(namespaceName)) {
            indent = "    ";
            contents.Add("namespace " + namespaceName);
            contents.Add("{");
        }

        contents.Add(indent + "public sealed class " + booterClassName + " : BootableMonoBehaviour");
        contents.Add(indent + "{");
        contents.Add(indent + "    public override void bootstrap(Action completion)");
        contents.Add(indent + "    {");
        contents.Add(indent + "        " + className + " itemToBoot = FindObjectOfType<" + className + ">();");
        contents.Add(indent + "        if (itemToBoot == null) {");
        contents.Add(indent + "            MDebug.LogError(\"[" + booterClassName + "] Cannot find " + className + " to boot it.\");");
        contents.Add(indent + "            return;");
        contents.Add(indent + "        }");
        contents.Add(indent + "        itemToBoot.bootstrap();");
        contents.Add(indent + "        completion();");
        contents.Add(indent + "    }");
        contents.Add("");
        contents.Add(indent + "    // public override void bootstrapDidComplete(Action completion)");
        contents.Add(indent + "    // {");
        contents.Add(indent + "    //     completion();");
        contents.Add(indent + "    // }");
        contents.Add(indent + "}");

        if (!string.IsNullOrEmpty(namespaceName)) {
            contents.Add("}");
        }

        File.WriteAllLines(booterScriptPath, contents.ToArray());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(booterScriptPath);
        UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(booterScriptPath, string.IsNullOrEmpty(namespaceName) ? 8 : 10);
    }

    [MenuItem("Assets/Create/C# Booter Script", true, 82)]
    private static bool CreateInspectorValidator()
    {
        return Selection.activeObject is MonoScript;
    }
}
