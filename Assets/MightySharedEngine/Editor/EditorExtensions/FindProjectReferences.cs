using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public static class FindProject
{
    [MenuItem("Assets/Log GUID To Console", false, 2000)]
    private static void LogGUID()
    {
        List<Object> objects = new List<Object>(Selection.objects);

        if (objects.Count == 0) {
            Debug.LogError("No assets currently selected in the Project window");
            return;
        }

        // Sort the selected objects first to make it easier to look through the console output when multiple objects are selected
        objects.Sort(delegate (Object itemA, Object itemB) {
            return itemA.name.CompareTo(itemB.name);
        });

        for (int i = 0; i < objects.Count; i++) {
            Debug.Log("GUID for asset " + objects[i].name + " is: " + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(objects[i])), objects[i]);
        }
    }

#if UNITY_EDITOR_OSX
    [MenuItem("Assets/Find References In Project", false, 2001)]
    private static void FindProjectReferences()
    {
        List<Object> objects = new List<Object>(Selection.objects);

        if (objects.Count == 0) {
            Debug.LogError("No assets currently selected in the Project window");
            return;
        }

        // Sort the selected objects first to make it easier to look through the console output when multiple objects are selected
        objects.Sort(delegate (Object itemA, Object itemB) {
            return itemA.name.CompareTo(itemB.name);
        });

        for (int i = 0; i < objects.Count; i++) {
            if (EditorUtility.DisplayCancelableProgressBar("Finding Project References", i.ToString() + " / " + objects.Count.ToString(), (float)i / objects.Count)) {
                break;
            }
            FindProjectReferences(objects[i]);
        }
        EditorUtility.ClearProgressBar();
    }

    private static void FindProjectReferences(Object selectedObject)
    {
        string appDataPath = Application.dataPath;
        string selectedAssetPath = AssetDatabase.GetAssetPath(selectedObject);
        string guid = AssetDatabase.AssetPathToGUID(selectedAssetPath);

        if (string.IsNullOrEmpty(selectedAssetPath) || string.IsNullOrEmpty(guid)) {
            Debug.LogError("Unable to find the selected object named '" + selectedObject.name + "' in the Asset Database. Do you perhaps have a Scene object selected instead of a Project object?");
            return;
        }

        List<string> references = FindProjectReferences(guid);

        if (selectedAssetPath.EndsWithFast(".cs")) {
            List<string> classNames = new List<string>();
            string[] lines = File.ReadAllLines(selectedAssetPath);
            foreach (string line in lines) {
                if (line.Contains("class")) {
                    // This is pretty bad. Is there a way to get all the possible class "modifiers"?
                    string className = line.Replace("class", "").Replace("{", "").Replace("public", "").Replace("protected", "").Replace("private", "").Replace("internal", "").Replace("static", "").Replace("abstract", "").Replace("partial", "").Replace("sealed", "");
                    if (className.Contains(":")) {
                        className = className.Substring(0, className.IndexOf(":"));
                    }
                    className = className.Trim();

                    if (className.Contains(" ")) {
                        Debug.LogError("Was not able to parse the following class declaration (Scott missed somthing):\n" + line);
                        continue;
                    }

                    if (!classNames.Contains(className))
                        classNames.Add(className);
                }
            }

            foreach (string className in classNames) {
                List<string> classReferences = FindProjectReferences(className);
                foreach (string classReference in classReferences) {
                    if (classReference.EndsWithFast(".cs") && !references.Contains(classReference))
                        references.Add(classReference);
                }
            }
        }

        // Don't care about the meta file of whatever we have selected or what we have selected
        references.Remove(selectedAssetPath);
        references.Remove(selectedAssetPath + ".meta");

        string output = "";
        foreach (var file in references) {
            output += file + "\n";
            Debug.Log(file, AssetDatabase.LoadMainAssetAtPath(file));
        }

        Debug.Log(references.Count + " references found for object " + selectedObject.name + "\n\n" + output, selectedObject);
    }

    private static List<string> FindProjectReferences(string searchString)
    {
        string appDataPath = Application.dataPath;
        List<string> references = new List<string>();

        var psi = new System.Diagnostics.ProcessStartInfo();
        psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Maximized;
        psi.FileName = "/usr/bin/mdfind";
        psi.Arguments = "-onlyin " + Application.dataPath + " " + searchString;
        psi.UseShellExecute = false;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;

        System.Diagnostics.Process process = new System.Diagnostics.Process();
        process.StartInfo = psi;

        process.OutputDataReceived += (sender, e) => {
            if (string.IsNullOrEmpty(e.Data))
                return;

            string relativePath = "Assets" + e.Data.Replace(appDataPath, "");
            if (!references.Contains(relativePath))
                references.Add(relativePath);
        };
        process.ErrorDataReceived += (sender, e) => {
            if (string.IsNullOrEmpty(e.Data))
                return;

            Debug.LogError(e.Data);
        };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.WaitForExit(10000);

        return references;
    }
#endif
}
