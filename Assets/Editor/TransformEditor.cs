using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Transform))]
public sealed class TransformEditor : Editor
{
    /// <summary>
    /// Draw the inspector widget.
    /// </summary>
    public override void OnInspectorGUI()
    {
        Transform transform = target as Transform;
        EditorGUIUtility.labelWidth = 15f;

        Vector3 position = transform.localPosition;
        Vector3 rotation = transform.localEulerAngles;
        Vector3 scale = transform.localScale;

        // Position
        EditorGUILayout.BeginHorizontal();
        {
            if (DrawButton("P", "Reset Position", IsResetPositionValid(transform), 20f))
            {
                position = Vector3.zero;
            }
            position = DrawVector3(position);
        }
        EditorGUILayout.EndHorizontal();

        // Rotation
        EditorGUILayout.BeginHorizontal();
        {
            if (DrawButton("R", "Reset Rotation", IsResetRotationValid(transform), 20f))
            {
                rotation = Vector3.zero;
            }
            rotation = DrawVector3(rotation);
        }
        EditorGUILayout.EndHorizontal();

        // Scale
        EditorGUILayout.BeginHorizontal();
        {
            if (DrawButton("S", "Reset Scale", IsResetScaleValid(transform), 20f))
            {
                scale = Vector3.one;
            }
            scale = DrawVector3(scale);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("s:-0.1"))
        {
            scale = scale - (scale * 0.1f);
        }
        if (GUILayout.Button("s:+0.1"))
        {
            scale = scale + (scale * 0.1f);
        }
        if (GUILayout.Button("s:*0.5"))
        {
            scale = scale * 0.5f;
        }
        if (GUILayout.Button("s:*2"))
        {
            scale = scale * 2f;
        }
        if (GUILayout.Button("s:*1.5"))
        {
            scale = scale * 1.5f;
        }
        EditorGUILayout.EndHorizontal();

        // If something changes, set the transform values
        if (GUI.changed)
        {
            Undo.RecordObject(transform, "Changed Transform Values");
            transform.localPosition = Validate(position);
            transform.localEulerAngles = Validate(rotation);
            transform.localScale = Validate(scale);
        }
    }

    /// <summary>
    /// Helper function that draws a button in an enabled or disabled state.
    /// </summary>
    static bool DrawButton(string title, string tooltip, bool enabled, float width)
    {
        if (enabled)
        {
            // Draw a regular button
            return GUILayout.Button(new GUIContent(title, tooltip), GUILayout.Width(width));
        }
        else
        {
            // Button should be disabled -- draw it darkened and ignore its return value
            Color color = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.25f);
            GUILayout.Button(new GUIContent(title, tooltip), GUILayout.Width(width));
            GUI.color = color;
            return false;
        }
    }

    /// <summary>
    /// Helper function that draws a field of 3 floats.
    /// </summary>
    static Vector3 DrawVector3(Vector3 value)
    {
        GUILayoutOption opt = GUILayout.MinWidth(30f);
        value.x = EditorGUILayout.FloatField("X", value.x, opt);
        value.y = EditorGUILayout.FloatField("Y", value.y, opt);
        value.z = EditorGUILayout.FloatField("Z", value.z, opt);
        return value;
    }

    /// <summary>
    /// Helper function that determines whether its worth it to show the reset position button.
    /// </summary>
    static bool IsResetPositionValid(Transform targetTransform)
    {
        Vector3 v = targetTransform.localPosition;
        return (v.x != 0f || v.y != 0f || v.z != 0f);
    }

    /// <summary>
    /// Helper function that determines whether its worth it to show the reset rotation button.
    /// </summary>
    static bool IsResetRotationValid(Transform targetTransform)
    {
        Vector3 v = targetTransform.localEulerAngles;
        return (v.x != 0f || v.y != 0f || v.z != 0f);
    }

    /// <summary>
    /// Helper function that determines whether its worth it to show the reset scale button.
    /// </summary>
    static bool IsResetScaleValid(Transform targetTransform)
    {
        Vector3 v = targetTransform.localScale;
        return (v.x != 1f || v.y != 1f || v.z != 1f);
    }

    /// <summary>
    /// Helper function that removes not-a-number values from the vector.
    /// </summary>
    static Vector3 Validate(Vector3 vector)
    {
        vector.x = float.IsNaN(vector.x) ? 0f : vector.x;
        vector.y = float.IsNaN(vector.y) ? 0f : vector.y;
        vector.z = float.IsNaN(vector.z) ? 0f : vector.z;
        return vector;
    }
}
