using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

public static class EditorUtilities
{
    public static void DeleteAsset(string path, bool saveAndRefreshAssets = false)
    {
        // Deleting files/folders directly can be faster than calling AssetDatabase.DeleteAsset because Unity doesn't have to do any extra processing for each file.
        // Just make sure to call AssetDatabase.SaveAssets and AssetDatabase.Refresh after finishing you DeleteAsset batch

        bool found = false;
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
            File.Delete(path + ".meta");
            found = true;
        }
        else if (File.Exists(path))
        {
            File.Delete(path);
            File.Delete(path + ".meta");
            found = true;
        }

        if (found && saveAndRefreshAssets)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    public static void DeleteAssetsThatMatch(string directoryPath, string filePattern, bool saveAndRefreshAssets = false)
    {
        // Deleting files/folders directly can be faster than calling AssetDatabase.DeleteAsset because Unity doesn't have to do any extra processing for each file.
        // Just make sure to call AssetDatabase.SaveAssets and AssetDatabase.Refresh after finishing you DeleteAsset batch

        bool found = false;
        string[] files = Directory.GetFiles(directoryPath, filePattern);
        int fileCount = files.Length;
        for (int i = 0; i < fileCount; i++)
        {
            if (!files[i].EndsWithFast(".meta"))
            {
                File.Delete(files[i]);
                File.Delete(files[i] + ".meta");
                found = true;
            }
        }

        if (found && saveAndRefreshAssets)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    public static T GetAsset<T>(string assetName) where T : UnityEngine.Object
    {
        string[] assetGUIDs = AssetDatabase.FindAssets(assetName + " t:" + typeof(T));
        for (int i = 0; i < assetGUIDs.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(assetGUIDs[i]);
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                return asset;
            }
        }

        return null;
    }

    public static T GetComponentFromPrefab<T>(string prefabName) where T : Component
    {
        string[] assetGUIDs = AssetDatabase.FindAssets(prefabName + " t:Prefab");
        for (int i = 0; i < assetGUIDs.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(assetGUIDs[i]);
            GameObject prefabToChange = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            T component = prefabToChange.GetComponent<T>();
            if (component != null)
            {
                return component;
            }
        }

        return null;
    }

    public static List<T> GetAllPrefabsWithComponent<T>() where T : Component
    {
        List<T> result = new List<T>();

        string[] assetGUIDs = AssetDatabase.FindAssets("t:Prefab");
        for (int i = 0; i < assetGUIDs.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(assetGUIDs[i]);
            GameObject prefabToChange = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            T component = prefabToChange.GetComponent<T>();
            if (component != null)
            {
                result.Add(component);
            }
        }

        return result;
    }

    public static List<T> GetAllPrefabsWithComponentRecursive<T>(bool includeUnityPackages = false) where T : Component
    {
        List<T> result = new List<T>();

        string[] assetGUIDs = AssetDatabase.FindAssets("t:Prefab");
        for (int i = 0; i < assetGUIDs.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(assetGUIDs[i]);
            if (!includeUnityPackages && assetPath.StartsWithFast("Packages")) continue;

            GameObject prefabToChange = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            T[] components = prefabToChange.GetComponentsInChildren<T>(true);
            for (int j = 0; j < components.Length; j++)
            {
                result.Add(components[j]);
            }
        }

        return result;
    }

    public static object GetParent(SerializedProperty prop)
    {
        string path = prop.propertyPath.Replace(".Array.data[", "[");
        object obj = prop.serializedObject.targetObject;
        string[] elements = path.Split('.');

        foreach (string element in elements.Take(elements.Length - 1))
        {
            if (element.Contains("["))
            {
                string elementName = element.Substring(0, element.IndexOf("["));
                int index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                obj = GetValue(obj, elementName, index);
            }
            else
            {
                obj = GetValue(obj, element);
            }
        }

        return obj;
    }

    private static object GetValue(object source, string name)
    {
        if (source == null)
        {
            return null;
        }

        Type type = source.GetType();
        FieldInfo f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

        if (f == null)
        {
            PropertyInfo p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (p == null)
            {
                return null;
            }

            return p.GetValue(source, null);
        }

        return f.GetValue(source);
    }

    private static object GetValue(object source, string name, int index)
    {
        IEnumerable enumerable = GetValue(source, name) as IEnumerable;
        IEnumerator enm = enumerable.GetEnumerator();
        bool movedNext = false;

        while (index-- >= 0)
        {
            movedNext = enm.MoveNext();
        }

        if (movedNext)
        {
            return enm.Current;
        }
        else
        {
            // Looks like we couldn't find the right value so the parent probably hasn't been created yet.
            // This can happen when adding a new object to a list that is the parent of a ReorderableList.
            return null;
        }
    }

    private static readonly Regex arrayElementRegex = new Regex(@"\.Array\.data\[([0-9]+)\]$");
    public static bool IsPropertyAnArrayElement(SerializedProperty property)
    {
        return arrayElementRegex.IsMatch(property.propertyPath);
    }

    public static int GetArrayIndexOfProperty(SerializedProperty property)
    {
        Match match = arrayElementRegex.Match(property.propertyPath);
        if (!match.Success) return -1;
        else return MathfExtensions.parseInt(match.Groups[1].Value, -1);
    }

    public static object GetTargetObjectOfProperty(SerializedProperty property)
    {
        string path = property.propertyPath.Replace(".Array.data[", "[");
        object obj = property.serializedObject.targetObject;
        string[] elements = path.Split('.');
        foreach (string element in elements)
        {
            if (element.Contains("["))
            {
                string elementName = element.Substring(0, element.IndexOf("["));
                int index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                obj = GetValue(obj, elementName, index);
            }
            else
            {
                obj = GetValue(obj, element);
            }
        }
        return obj;
    }

    public static void DoArrayElementPropertyContextMenu(SerializedProperty property)
    {
        GenericMenu genericMenu = new GenericMenu();
        SerializedProperty serializedProperty = property.serializedObject.FindProperty(property.propertyPath);
        AddMenuItems(property, genericMenu);

        if (property.propertyPath.LastIndexOf(']') == property.propertyPath.Length - 1)
        {
            string propertyPath = property.propertyPath.Substring(0, property.propertyPath.LastIndexOf(".Array.data["));
            SerializedProperty serializedProperty2 = property.serializedObject.FindProperty(propertyPath);
            if (!serializedProperty2.isFixedBuffer)
            {
                genericMenu.AddItem(new GUIContent("Duplicate Array Element"), false, delegate (object a)
                {
                    DuplicateArrayElement(a);
                    EditorGUIUtility.editingTextField = false;
                }, serializedProperty);
                genericMenu.AddItem(new GUIContent("Delete Array Element"), false, delegate (object a)
                {
                    DeleteArrayElement(a);
                    EditorGUIUtility.editingTextField = false;
                }, serializedProperty);
            }
        }
        if (Event.current.shift)
        {
            if (genericMenu.GetItemCount() > 0)
            {
                genericMenu.AddSeparator("");
            }
            genericMenu.AddItem(new GUIContent("Print Property Path"), false, delegate (object e)
            {
                Debug.Log(((SerializedProperty)e).propertyPath);
            }, serializedProperty);
        }
        if (EditorApplication.contextualPropertyMenu != null)
        {
            if (genericMenu.GetItemCount() > 0)
            {
                genericMenu.AddSeparator("");
            }
            EditorApplication.contextualPropertyMenu(genericMenu, property);
        }
        Event.current.Use();
        if (genericMenu.GetItemCount() != 0)
        {
            genericMenu.ShowAsContext();
        }
    }

    private static void AddMenuItems(SerializedProperty property, GenericMenu menu)
    {
        Type scriptAttributeUtilityType = Type.GetType("UnityEditor.ScriptAttributeUtility,UnityEditor");
        Type propertyHandlerType = Type.GetType("UnityEditor.PropertyHandler,UnityEditor");

        MethodInfo getHandlerMethod = scriptAttributeUtilityType.GetMethod("GetHandler", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        MethodInfo addMenuItemsMethod = propertyHandlerType.GetMethod("AddMenuItems", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

        object propertyHandler = getHandlerMethod.Invoke(null, new object[] { property });
        addMenuItemsMethod.Invoke(propertyHandler, new object[] { property, menu });
    }

    public static void DuplicateArrayElement(object userData)
    {
        SerializedProperty serializedProperty = (SerializedProperty)userData;
        serializedProperty.DuplicateCommand();
        serializedProperty.serializedObject.ApplyModifiedProperties();
        ForceReloadInspectors();
    }

    public static void DeleteArrayElement(object userData)
    {
        SerializedProperty serializedProperty = (SerializedProperty)userData;
        serializedProperty.DeleteCommand();
        serializedProperty.serializedObject.ApplyModifiedProperties();
        ForceReloadInspectors();
    }

    public static void ForceReloadInspectors()
    {
        Type editorUtilityType = typeof(EditorUtility);
        MethodInfo forceReloadInspectorsMethod = editorUtilityType.GetMethod("ForceReloadInspectors", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        forceReloadInspectorsMethod.Invoke(null, null);
    }

    public static int ArrayPropertyLineCount(SerializedProperty property)
    {
        if (property.isExpanded)
        {
            return (2 + property.arraySize);
        }
        else
        {
            return 1;
        }
    }

    public static void DrawFloatField(Rect position, SerializedProperty property, string label, Action onChange = null)
    {
        EditorGUI.BeginChangeCheck();
        float value = EditorGUI.FloatField(position, new GUIContent(label), property.floatValue);
        if (EditorGUI.EndChangeCheck())
        {
            property.floatValue = value;
            if (onChange != null) onChange();
        }
    }

    public static void DrawTextField(Rect position, SerializedProperty property, string label, Action onChange = null)
    {
        EditorGUI.BeginChangeCheck();
        string value = EditorGUI.TextField(position, new GUIContent(label), property.stringValue);
        if (EditorGUI.EndChangeCheck())
        {
            property.stringValue = value;
            if (onChange != null) onChange();
        }
    }

    public static void DrawPropertyField(Rect position, SerializedProperty property, string label, Action onChange = null)
    {
        EditorGUI.BeginChangeCheck();
        EditorGUI.PropertyField(position, property, new GUIContent(label), true);
        if (EditorGUI.EndChangeCheck())
        {
            if (onChange != null) onChange();
        }
    }

    public static void DrawCurveField(Rect position, SerializedProperty property, string label, Action onChange = null)
    {
        EditorGUI.BeginChangeCheck();
        AnimationCurve value = EditorGUI.CurveField(position, new GUIContent(label), property.animationCurveValue);
        if (EditorGUI.EndChangeCheck())
        {
            property.animationCurveValue = value;
            if (onChange != null) onChange();
        }
    }

    public static void DrawObjectField(Rect position, SerializedProperty property, string label, Action onChange = null)
    {
        EditorGUI.BeginChangeCheck();
        EditorGUI.ObjectField(position, property, new GUIContent(label));
        if (EditorGUI.EndChangeCheck())
        {
            if (onChange != null) onChange();
        }
    }

    public static void DrawToggleField(Rect position, SerializedProperty property, string label, Action onChange = null)
    {
        EditorGUI.BeginChangeCheck();
        bool value = EditorGUI.Toggle(position, new GUIContent(label), property.boolValue);
        if (EditorGUI.EndChangeCheck())
        {
            property.boolValue = value;
            if (onChange != null) onChange();
        }
    }

    // public static int VerifyUnityEventTarget(UnityEvent target, GameObject targetObject)
    // {
    //     int failedEventCount = 0;

    //     UnityEngine.Object targetPrefabParent = PrefabUtility.GetPrefabObject(targetObject);
    //     string prefabPath = $" Owning prefab path: [{AssetDatabase.GetAssetPath(targetPrefabParent)}]";

    //     for (int i = 0; i < target.GetPersistentEventCount(); i++) {
    //         string name = target.GetPersistentMethodName(i);
    //         object listener = target.GetPersistentTarget(i);

    //         if (listener == null && !prefabPath.Contains("LevelEditor")) { //piffle: we ignore events from our editor prefabs folder
    //             Debug.LogError($"[UnityEvent Method Verification] Target: [{targetObject.name}]. Null listener object when looking for method: [{name}] {prefabPath}");
    //             failedEventCount++;
    //             continue;
    //         }
    //         if (string.IsNullOrEmpty(name) && !prefabPath.Contains("LevelEditor")) { //piffle: we ignore events from our editor prefabs folder
    //             Debug.LogError($"[UnityEvent Method Verification] Target: [{targetObject.name}]. Empty method name on listener: [{listener}] {prefabPath}");
    //             failedEventCount++;
    //             continue;
    //         }

    //         MethodInfo methodInfo = null;
    //         PersistentListenerMode modeUsed = PersistentListenerMode.Void;

    //         foreach (KeyValuePair<PersistentListenerMode, Type[]> option in argumentOptions) {
    //             methodInfo = UnityEvent.GetValidMethodInfo(listener, name, option.Value);

    //             if (methodInfo != null) {
    //                 modeUsed = option.Key;
    //                 break;
    //             }
    //         }

    //         if (methodInfo == null && !prefabPath.Contains("LevelEditor")) { //piffle: we ignore events from our editor prefabs folder
    //             Debug.LogError($"[UnityEvent Method Verification]. Target: [{targetObject.name}] failed to find method: [{name}] on object: [{listener}] {prefabPath}");
    //             failedEventCount++;
    //         }
    //     }

    //     return failedEventCount;
    // }

    private static readonly Dictionary<PersistentListenerMode, Type[]> argumentOptions = new Dictionary<PersistentListenerMode, Type[]>()
    {
        { PersistentListenerMode.Bool, new[] {typeof(bool)} },
        { PersistentListenerMode.EventDefined, new Type[]{} },
        { PersistentListenerMode.Float, new[] {typeof(float)} },
        { PersistentListenerMode.Int, new[] {typeof(int)} },
        { PersistentListenerMode.Object, new[] {typeof(UnityEngine.Object)} },
        { PersistentListenerMode.String, new[] { typeof(string)} },
        { PersistentListenerMode.Void, new Type[0] }
    };
}
