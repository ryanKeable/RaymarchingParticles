using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(LayerAttribute))]
public sealed class LayerAttributePropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        property.intValue = EditorGUI.LayerField(position, label, property.intValue);
    }
}
