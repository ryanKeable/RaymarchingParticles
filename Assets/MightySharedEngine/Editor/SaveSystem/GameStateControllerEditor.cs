using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

[CustomEditor(typeof(GameStateController))]
public class GameStateControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        GameStateController controller = target as GameStateController;
        // string filePath = GameStateController.getStateFilePath();

        EditorGUILayout.Space();

        if (Application.isPlaying)
        {
            if (GUILayout.Button("Dump Current State"))
            {
                string file = SystemHelper.documentFilePath("persistentStateDump.txt");
                controller.dumpState(file);
                EditorUtility.RevealInFinder(file);
            }
            if (GUILayout.Button("Save Current State"))
            {
                string file = SystemHelper.documentFilePath("persistentStateDump.txt");
                controller.saveState();
                EditorUtility.RevealInFinder(file);
            }


            EditorGUILayout.Space();
            EditorGUILayout.LabelField("-------");

            // Show all state
            List<string> keys = controller.allStateKeys();
            keys.Sort();
            for (int i = 0; i < keys.Count; i++)
            {
                addStateLine(keys[i]);
            }

            EditorGUILayout.LabelField("-------");

            GameStateController.gameIsPaused = EditorGUILayout.Toggle("Game is paused:", GameStateController.gameIsPaused);
            GameStateController.gameSpeed = EditorGUILayout.FloatField("Game Speed:", GameStateController.gameSpeed);

            EditorGUILayout.LabelField("-------");
            EditorGUILayout.Space();
        }
        else
        {
            // if (GUILayout.Button("Show Last State"))
            // {
            //     string stateFile = SystemHelper.documentFilePath(filePath);
            //     PersistentStorage stateHolder = PersistentStorage.storageFromLocalFile(stateFile);
            //     string file = SystemHelper.documentFilePath("persistentStateDump.txt");
            //     stateHolder.saveToFile(file);
            //     EditorUtility.RevealInFinder(file);
            // }
            // if (GUILayout.Button("Save Last State To TEST1"))
            // {
            //     string stateFile = SystemHelper.documentFilePath(filePath);
            //     string source = PersistentStorage.lastUpdatedFile(stateFile);
            //     string dest = SystemHelper.documentFilePath("TEST1");
            //     File.Copy(source, dest, true);
            // }
            // if (GUILayout.Button("Load to TEST1"))
            // {
            //     string source = SystemHelper.documentFilePath("TEST1");
            //     string directory = Path.GetDirectoryName(source);
            //     for (int i = 0; i <= 5; i++)
            //     {
            //         string dest = Path.Combine(directory, filePath + ".2." + i.ToString());
            //         File.Copy(source, dest, true);
            //     }
            // }
            // if (GUILayout.Button("Save Last State To TEST 2"))
            // {
            //     string stateFile = SystemHelper.documentFilePath(filePath);
            //     string source = PersistentStorage.lastUpdatedFile(stateFile);
            //     string dest = SystemHelper.documentFilePath("TEST2");
            //     File.Copy(source, dest, true);
            // }
            // if (GUILayout.Button("Load to TEST2"))
            // {
            //     string source = SystemHelper.documentFilePath("TEST2");
            //     string directory = Path.GetDirectoryName(source);
            //     for (int i = 0; i <= 5; i++)
            //     {
            //         string dest = Path.Combine(directory, filePath + ".2." + i.ToString());
            //         File.Copy(source, dest, true);
            //     }
            // }
        }

        EditorGUILayout.Space();

        DrawDefaultInspector();
    }

    void addStateLine(string key)
    {
        string currentValue = GameStateController.playerStringForKey(key);
        string theString = EditorGUILayout.TextField(key, currentValue);
        if (theString != currentValue) GameStateController.setPlayerStringForKey(key, theString);
    }
}
